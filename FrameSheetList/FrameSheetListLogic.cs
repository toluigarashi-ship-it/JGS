namespace DesktopApp.FrameSheetList;

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.SqlClient;

/// <summary>
/// 車台番号連絡票一覧画面のロジック
/// </summary>
internal sealed class FrameSheetListLogic
{

    #region プライベート変数

    private const string SelectSql = """
    SELECT
        ID,
        CSVTYP,
        CSVTYPNM,
        CRTDTTM,
        IMPDT,
        HCHKYKNO,
        FRMNO,
        FRMSERNO,
        STRNM1,
        STRNM2,
        COALESCE(STRNM1, N'') + COALESCE(STRNM2, N'') AS STRNM,
        KKYKNM,
        STATUS,
        STATUSNM,
        TNTUT,
        TNTUTNM
    FROM dbo.JGSVOCRLST
    """;

    private const string SummarySql = """
    SELECT
        COUNT(1) AS CNT_TOTAL,
        SUM(CASE WHEN STATUS IN (9, 99) THEN 1 ELSE 0 END) AS CNT_KAKUNINMAE,
        SUM(CASE WHEN STATUS = 1 THEN 1 ELSE 0 END) AS CNT_ICHIZON
    FROM dbo.JGSVOCRLST
    """;

    private readonly string _connectionString;
    private readonly string _userId;

    #endregion

    #region コンストラクタ

    /// <summary>
    /// FrameSheetListLogicの新しいインスタンスを初期化する
    /// </summary>
    /// <param name="connectionString">DB接続文字列</param>
    /// <param name="userId">処理実行ユーザーID</param>
    internal FrameSheetListLogic(string connectionString, string userId)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        _userId = userId;
    }

    #endregion

    #region 型定義

    /// <summary>
    /// 文字列検索の一致方法を表す列挙
    /// </summary>
    internal enum TextMatchMode
    {
        /// <summary>前方一致（値%）</summary>
        Prefix,

        /// <summary>後方一致（%値）</summary>
        Suffix,

        /// <summary>部分一致（%値%）</summary>
        Contains,

        /// <summary>完全一致</summary>
        Exact,
    }

    #endregion

    #region メソッド

    /// <summary>
    /// 検索条件に基づいて一覧を再取得する（検索ボタン用）
    /// </summary>
    /// <param name="vm">画面ViewModel</param>
    /// <param name="ct">キャンセルトークン</param>
    /// <returns>検索処理の完了を表すTask</returns>
    internal async Task SearchAsync(FrameSheetListViewModel vm, CancellationToken ct = default)
    {
        if (vm is null)
        {
            throw new ArgumentNullException(nameof(vm));
        }

        var cond = vm.Conditions ?? new FrameSheetListSearchConditions();

        var items = await FetchItemsFromViewByConditionsAsync(cond, ct).ConfigureAwait(false);
        var summary = await FetchSummaryFromViewAsync(ct).ConfigureAwait(false);

        vm.Items = items;

        vm.Summary.CNT_TOTAL = items.Count;
        vm.Summary.CNT_KAKUNINMAE = summary.CNT_KAKUNINMAE;
        vm.Summary.CNT_ICHIZON = summary.CNT_ICHIZON;
    }

    #endregion

    #region ヘルパ関数

    /// <summary>
    /// 検索条件 <paramref name="cond"/> から、WHERE句の条件リストとDapper用パラメータを生成します。
    /// 文字列条件は前方一致（LIKE '値%'）として追加し、取込日は From を含み、To は当日中を含めるため翌日0:00未満（&lt; To+1日）で絞り込みます。
    /// また、種別・担当・ステータスなどの複数選択条件は IN 句として追加します。
    /// </summary>
    /// <param name="cond">検索条件。</param>
    /// <returns>WHERE句条件リストとパラメータのタプル。</returns>
    private static (List<string> WhereClauses, DynamicParameters Parameters) BuildWhere(FrameSheetListSearchConditions cond)
    {
        var where = new List<string>();
        var p = new DynamicParameters();

        // 文字列
        AddTextMatch(where, p, "HCHKYKNO", "CondHCHKYKNO", cond.CondHCHKYKNO, TextMatchMode.Exact);
        AddTextMatch(where, p, "FRMNO", "CondFRMNO", cond.CondFRMNO, TextMatchMode.Prefix);
        AddTextMatch(where, p, "FRMSERNO", "CondFRMSERNO", cond.CondFRMSERNO, TextMatchMode.Prefix);
        AddTextMatch(where, p, "COALESCE(STRNM1, N'') + COALESCE(STRNM2, N'')", "CondSTRNM", cond.CondSTRNM, TextMatchMode.Contains);
        AddTextMatch(where, p, "KKYKNM", "CondKKYKNM", cond.CondKKYKNM, TextMatchMode.Contains);

        // 取込日
        if (cond.CondIMPDTFrom is DateTime from)
        {
            where.Add("IMPDT >= @CondIMPDTFrom");
            p.Add("@CondIMPDTFrom", from.Date);
        }

        if (cond.CondIMPDTTo is DateTime to)
        {
            // Toは当日中まで含める（翌日0:00未満）
            where.Add("IMPDT < @CondIMPDTToNext");
            p.Add("@CondIMPDTToNext", to.Date.AddDays(1));
        }

        // 種別（CSVTYP）
        var csvTypLst = new List<int>();
        if (cond.CondCSVTYP_Normal)
        {
            csvTypLst.Add(1);
        }

        if (cond.CondCSVTYP_Tmt)
        {
            csvTypLst.Add(2);
        }

        if (csvTypLst.Count > 0)
        {
            where.Add("CSVTYP IN @CsvTyps");
            p.Add("@CsvTyps", csvTypLst);
        }

        // 担当UT（TNTUT）
        var tntUTLst = new List<int>();
        if (cond.CondTNTUT_Register)
        {
            tntUTLst.Add(1);
        }

        if (cond.CondTNTUT_Document)
        {
            tntUTLst.Add(2);
        }

        if (tntUTLst.Count > 0)
        {
            where.Add("TNTUT IN @TntUTs");
            p.Add("@TntUTs", tntUTLst);
        }

        // ステータス（STATUS）
        var statusLst = new List<int>();
        if (cond.CondSTS_Confirmed)
        {
            statusLst.Add(0);
        }

        if (cond.CondSTS_Temporary)
        {
            statusLst.Add(1);
        }

        if (cond.CondSTS_ResendRequest)
        {
            statusLst.Add(2);
        }

        if (cond.CondSTS_Unregistered)
        {
            statusLst.Add(99);
        }

        if (statusLst.Count > 0)
        {
            where.Add("STATUS IN @Statuses");
            p.Add("@Statuses", statusLst);
        }

        return (where, p);
    }

    /// <summary>
    /// ベースとなるSQL文にWHERE句および固定のORDER BY句を付加し、
    /// 実行用の完成SQL文字列を生成します。
    /// WHERE条件が存在する場合のみ、AND区切りで結合して追加します。
    /// </summary>
    /// <param name="baseSql">SELECT句などを含む基礎となるSQL文。</param>
    /// <param name="whereClauses">WHERE句に追加する条件文字列のリスト。</param>
    /// <returns>WHERE句およびORDER BY句を付加した完成SQL文字列。</returns>
    private static string ComposeSql(string baseSql, List<string> whereClauses)
    {
        var sql = baseSql;

        if (whereClauses.Count > 0)
        {
            sql += "\nWHERE " + string.Join("\n  AND ", whereClauses);
        }

        sql += "\nORDER BY IMPDT ASC, ID ASC;";
        return sql;
    }

    /// <summary>
    /// 複数の検索値に対して一致条件をWHERE句へ追加する
    /// </summary>
    /// <param name="where">WHERE句の条件を蓄積するリスト</param>
    /// <param name="p">Dapperに渡すパラメータ</param>
    /// <param name="column">対象カラム名</param>
    /// <param name="paramPrefix">パラメータ名の接頭辞</param>
    /// <param name="values">検索値の列</param>
    /// <param name="mode">一致方法</param>
    private static void AddTextMatch(
        List<string> where, DynamicParameters p, string column, string paramPrefix, IEnumerable<string?>? values, TextMatchMode mode)
    {
        if (values is null)
        {
            return;
        }

        var list = values
            .Select(v => v?.Trim())
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .ToList();

        if (list.Count == 0)
        {
            return;
        }

        var isExact = mode == TextMatchMode.Exact;
        var op = isExact ? "=" : "LIKE";

        if (list.Count == 1)
        {
            var key = "@" + paramPrefix;

            where.Add(isExact
                ? $"{column} {op} {key}"
                : $"{column} {op} {key} ESCAPE '\\'");

            p.Add(key, BuildTextMatchValue(list[0] !, mode));
            return;
        }

        var ors = new List<string>(list.Count);

        for (var i = 0; i < list.Count; i++)
        {
            var key = $"@{paramPrefix}{i}";

            ors.Add(isExact
                ? $"{column} {op} {key}"
                : $"{column} {op} {key} ESCAPE '\\'");

            p.Add(key, BuildTextMatchValue(list[i] !, mode));
        }

        where.Add("(" + string.Join(" OR ", ors) + ")");
    }

    /// <summary>
    /// 単一の検索値に対して一致条件をWHERE句へ追加する
    /// </summary>
    /// <param name="where">WHERE句の条件を蓄積するリスト</param>
    /// <param name="p">Dapperに渡すパラメータ</param>
    /// <param name="column">対象カラム名</param>
    /// <param name="paramName">パラメータ名</param>
    /// <param name="value">検索値</param>
    /// <param name="mode">一致方法</param>
    private static void AddTextMatch(
        List<string> where, DynamicParameters p, string column, string paramName, string? value, TextMatchMode mode)
        => AddTextMatch(where, p, column, paramName, new[] { value }, mode);

    private static string EscapeLikeValue(string value)
    {
        return value
            .Replace("\\", "\\\\") // 先にエスケープ文字自体を処理
            .Replace("%", "\\%")
            .Replace("_", "\\_")
            .Replace("[", "\\[");
    }

    /// <summary>
    /// 一致方法に応じてLIKE用の検索値を組み立てる
    /// </summary>
    /// <param name="value">検索値</param>
    /// <param name="mode">一致方法</param>
    /// <returns>LIKE句に渡す検索値</returns>
    /// <exception cref="ArgumentOutOfRangeException">未定義の一致方法が指定された場合</exception>
    private static string BuildTextMatchValue(string value, TextMatchMode mode)
    {
        var escaped = EscapeLikeValue(value);

        return mode switch
        {
            TextMatchMode.Prefix => escaped + "%",
            TextMatchMode.Suffix => "%" + escaped,
            TextMatchMode.Contains => "%" + escaped + "%",
            TextMatchMode.Exact => escaped,
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null),
        };
    }

    #endregion

    #region データアクセス

    /// <summary>
    /// 指定した検索条件 <paramref name="cond"/> からWHERE句とパラメータを生成し、
    /// ベースSQLに組み込んだクエリを実行して、ビューから一覧行データを非同期で取得します。
    /// 取得結果は <see cref="FrameSheetListRowViewModel"/> のリストとして返します。
    /// </summary>
    /// <param name="cond">検索条件。</param>
    /// <param name="ct">処理を中断するためのキャンセルトークン。</param>
    /// <returns>条件に一致した一覧行データのリスト。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="cond"/> が <c>null</c> の場合。</exception>
    private async Task<List<FrameSheetListRowViewModel>> FetchItemsFromViewByConditionsAsync(
        FrameSheetListSearchConditions cond,
        CancellationToken ct)
    {
        if (cond is null)
        {
            throw new ArgumentNullException(nameof(cond));
        }

        // 条件 → WHERE句 + パラメータ
        var (whereClauses, parameters) = BuildWhere(cond);

        // SQL組み立て（全体像：Base → WHERE → ORDER）
        var sql = ComposeSql(SelectSql, whereClauses);

        using var con = new SqlConnection(_connectionString);

        var cmd = new CommandDefinition(
            commandText: sql,
            parameters: parameters,
            commandType: CommandType.Text,
            commandTimeout: 10,
            cancellationToken: ct);

        var rows = await con.QueryAsync<FrameSheetListRowViewModel>(cmd).ConfigureAwait(false);
        return rows.ToList();
    }

    /// <summary>
    /// 一覧ビュー全件を対象に、画面上部表示用の件数サマリを取得します。
    /// 検索条件には依存しません。
    /// </summary>
    /// <param name="ct">処理を中断するためのキャンセルトークン。</param>
    /// <returns>件数サマリ。</returns>
    private async Task<FrameSheetListSummary> FetchSummaryFromViewAsync(CancellationToken ct)
    {
        using var con = new SqlConnection(_connectionString);

        var cmd = new CommandDefinition(
            commandText: SummarySql,
            commandType: CommandType.Text,
            commandTimeout: 10,
            cancellationToken: ct);

        var summary = await con.QuerySingleAsync<FrameSheetListSummary>(cmd).ConfigureAwait(false);
        return summary;
    }

    #endregion
}
