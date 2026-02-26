using Common.Db;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Text;

namespace DesktopApp.FrameSheetCheck;

/// <summary>
/// フレーム連絡票確認画面のロジック
/// </summary>
internal class FrameSheetCheckLogic
{

    // 車台番号連絡票CSV取込テーブル(OCR)取得
    private const string SelectOcrSql = """
        SELECT
            FCOID,
            FCOIMPFILENM,
            FCOIMPRCDCNT,
            FCOFILENM,
            FCOFORMNM,
            FCOSTRNM1,
            FCOSTRNM2,
            FCOSTROFSNM,
            FCOSTRPSTNO,
            FCOSTRADD1,
            FCOSTRADD2,
            FCOSTRTEL,
            FCOKYKNO,
            FCOCARNM,
            FCOKATA,
            FCOBUNM,
            FCOTNTSTFNM,
            FCOKKYKNM1,
            FCOKKYKNM2,
            FCOOSS,
            FCOSTRTNT,
            FCOSTREML,
            FCOENTDAY,
            FCOSYUJI,
            FCOSYUEI,
            FCOSYUJO,
            FCOSYU4NO,
            FCOSYUKEI,
            FCOSYU2TO,
            FCOSYU2TU,
            FCOSYU8TO,
            FCOSYUKND,
            FCOSYUSPL,
            FCOSYUSHR,
            FCOSYUYOT,
            FCOFRMNO,
            FCOFRMSERNO,
            FCOTRKYTI,
            FCONSYYTI,
            FCOBSEUSE0,
            FCOBSEUSE1,
            FCOBSEUSE2,
            FCOBSEUSE3,
            FCOBSEUSECRT,
            FCODOCARV,
            FCOAMPM,
            FCOBIKO,
            FCOJCHSUBNO,
            FCOAM,
            FCOPM,
            FCOSYUNM1,
            FCOSYUNM2,
            FCOENTDAYREAD,
            FCOOCRDAY,
            FCOOCRDTTM,
            FCOIMPDT,
            FCOSTS
        FROM dbo.JGSTFRMCSVOCR
        WHERE FCOID = @FCOID;
        """;

    //接続文字列
    private string _connectionString;
    //ログインユーザーID
    private string _userId;

    /// <summary>
    /// FrameSheetCheckLogicの新しいインスタンスを初期化する
    /// </summary>
    /// <param name="connectionString">DB接続文字列</param>
    /// <param name="userId">処理実行ユーザーID</param>
    internal FrameSheetCheckLogic(string connectionString, string userId)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        _userId = userId;
    }

    /// <summary>
    /// 確認画面の入力内容を保存する
    /// <para>
    /// 変更フラグ（CHK列）を再計算して確認テーブルを更新する
    /// </para>
    /// </summary>
    /// <param name="saveModel">保存対象データ</param>
    public void Save(FrameSheetCheckSaveModel saveModel)
    {
        using var db = new DbAccess(_connectionString);

        try
        {
            db.BeginTransaction();

            //取込テーブルを取得（比較用）
            var ocr_org = LoadOcr(db, saveModel.FOCFCOID);

            //取込テーブルの値を変換（ラジオボタン値等）
            var ocr_new = CreateSaveModelFromOcr(ocr_org);

            //CHG項目セット
            UpdateChangedFlags(saveModel, ocr_new);

            // 5) UPDATE
            UpdateChk(db, saveModel);

            db.Commit();
        }
        catch
        {
            db.Rollback();
            throw;
        }
    }

    /// <summary>
    /// 確認画面の初期表示処理を行う
    /// <para>
    /// 取込テーブルから対象データを取得し、
    /// 確認テーブルに存在しない場合は新規作成したうえで
    /// 確認テーブルのデータを取得する
    /// </para>
    /// </summary>
    /// <param name="fcoid">取込ID</param>
    /// <returns>
    /// 初期表示用の View
    /// 対象データが存在しない場合は null
    /// </returns>
    internal FrameSheetCheckViewModel? LoadInitial(int fcoid)
    {
        using var db = new DbAccess(_connectionString);

        db.BeginTransaction();
        try
        {
            //1.取込テーブル存在確認
            var ocr = db.Query<FrameSheetOcrRow>(
                @"
                SELECT
                    FCOID,
                    FCOFRMNO,
                    FCOFRMSERNO
                FROM dbo.JGSTFRMCSVOCR
                WHERE FCOID = @FCOID;
                ", new { FCOID = fcoid }).FirstOrDefault();

            if (ocr is null)
            {
                db.Commit();
                return null;
            }

            //2.確認テーブル存在確認
            var chkExists = db.ExecuteScalar<int>(
                @"
                SELECT COUNT(1)
                FROM JGSTFRMCSVOCRCHK
                WHERE FOCFCOID = @FOCFCOID;
                ", new { FOCFCOID = fcoid });

            //3.無ければ FCO → FOC マッピングして INSERT
            if (chkExists == 0)
            {
                try
                {
                    InsertFromOcr(db, fcoid);
                }
                catch (SqlException ex) when (ex.Number is 2627 or 2601)
                {
                    // 同時起動で既に作成済み
                }
            }

            //4.確認テーブルを取得してView にマッピング
            var view = db.Query<FrameSheetCheckViewModel>(
                @"
                SELECT
                    FOCFCOID,
                    FOCIMPFILENM,
                    FOCIMPRCDCNT,
                    FOCFILENM,
                    FOCFORMNM,
                    FOCSTRNM1,
                    FOCSTRNM2,
                    FOCSTROFSNM,
                    FOCSTRPSTNO,
                    FOCSTRADD1,
                    FOCSTRADD2,
                    FOCSTRTEL,
                    FOCKYKNO,
                    FOCCARNM,
                    FOCKATA,
                    FOCBUNM,
                    FOCTNTSTFNM,
                    FOCKKYKNM1,
                    FOCKKYKNM2,
                    FOCOSS,
                    FOCSTRTNT,
                    FOCSTREML,
                    FOCENTDAY,
                    FOCSYUJI,
                    FOCSYUEI,
                    FOCSYUJO,
                    FOCSYU4NO,
                    FOCSYUKEI,
                    FOCSYU2TO,
                    FOCSYU2TU,
                    FOCSYU8TO,
                    FOCSYUKND,
                    FOCSYUSPL,
                    FOCSYUSHR,
                    FOCSYUYOT,
                    FOCFRMNO,
                    FOCFRMSERNO,
                    FOCTRKYTI,
                    FOCNSYYTI,
                    FOCBSEUSE0,
                    FOCBSEUSE1,
                    FOCBSEUSE2,
                    FOCBSEUSE3,
                    FOCBSEUSECRT,
                    FOCDOCARV,
                    FOCAMPM,
                    FOCBIKO,
                    FOCJCHSUBNO,
                    FOCAM,
                    FOCPM,
                    FOCSYUNM1,
                    FOCSYUNM2,
                    FOCENTDAYREAD,
                    FOCOCRDAY,
                    FOCOCRDTTM,
                    FOCIMPDT,
                    FOCTMTNSY,
                    FOCTMTPUP,
                    FOCKYKNOCNF,
                    FOCISLAND,
                    FOCDOCARVCNF,
                    FOCMEMO01,
                    FOCMEMO02,
                    FOCMEMO03,
                    FOCMEMO04,
                    FOCSTS,
                    FOCTENMATSU,
                    -- フラグ
                    FOCKYKNOCHG,
                    FOCFRMNOCHG,
                    FOCFRMSERNOCHG,
                    FOCSTRTNTCHG,
                    FOCSYUJICHG,
                    FOCSYUJOCHG,
                    FOCTRKYTICHG,
                    FOCNSYYTICHG,
                    FOCBSEUSE0CHG,
                    FOCBSEUSE1CHG,
                    FOCBSEUSE2CHG,
                    FOCDOCARVCHG,
                    -- 監査
                    FOCCNFDATE,
                    FOCCNFUSR,
                    FOCINSDATE,
                    FOCINSUSR,
                    FOCUPDDATE,
                    FOCUPDUSR
                FROM JGSTFRMCSVOCRCHK
                WHERE FOCFCOID = @FOCFCOID;
                ", new { FOCFCOID = fcoid }).FirstOrDefault();

            if (view is null)
            {
                db.Commit();
                return null;
            }

            //5.最新の契約Noの取得→契約情報のセット
            if (!string.IsNullOrEmpty(view.FOCKYKNO))
            {
                SetKykInfo(db, view, view.FOCKYKNO);
            }

            //顛末コンボボックスの表示値をセット
            var comboItems = LoadComboItems(db);
            view.ComboItems = comboItems;

            // 修正済み表示用フラグセット
            SetChangedFlags(view);

            // 書類UT用表示判定
            // 取込テーブル.車両特定番号が入っていて、車台番号が空のとき車両UT用表示をする
            bool isUt = !string.IsNullOrWhiteSpace(ocr.FCOFRMSERNO)
                        && string.IsNullOrWhiteSpace(ocr.FCOFRMNO);

            // Viewへ表示制御情報をセット
            view.IsDocumentUtMode = isUt;

            db.Commit();
            return view;
        }
        catch
        {
            db.Rollback();
            throw;
        }
    }

    /// <summary>
    /// 契約情報をviewにセットする
    /// </summary>
    /// <param name="db">Dbアクセス</param>
    /// <param name="view">Viewモデル（契約情報部分のみ使用）</param>
    /// <param name="inputKykNo">契約No</param>
    /// <returns>True:成功, False:失敗</returns>
    internal bool SetKykInfo(DbAccess db, FrameSheetCheckViewModel view, string inputKykNo)
    {
        view.IsKykinfoExists = false;

        // 最新の契約No取得
        string latestKykNo = GetLatestKeiyakuNo(db, inputKykNo);
        if (string.IsNullOrWhiteSpace(latestKykNo))
        {
            return false;
        }

        // 契約情報取得
        var kyk = GetKeiyakutInfo(db, latestKykNo);
        if (kyk == null)
        {
            //契約情報クリア
            view.Kyk_LSKYKNO = null;
            view.Kyk_SHDNCARNM = null;
            view.Kyk_KATA = null;
            view.Kyk_EGYTNTBUCD = null;
            view.Kyk_EGYTNTNM = null;
            view.Kyk_TRKTNTNM = null;
            view.Kyk_KYKSKNMJDN = null;
            view.Kyk_NTINCHK_DS = null;
            view.Kyk_HCHSKNMJDN = null;
            return false;
        }

        // ---- view.Kyk_XXX にセット ----
        view.IsKykinfoExists = true;
        view.Kyk_LSKYKNO = latestKykNo;
        view.Kyk_SHDNCARNM = kyk.SHDNCARNM;
        view.Kyk_KATA = kyk.KATA;
        view.Kyk_EGYTNTBUCD = kyk.EGYTNTBUCD;
        view.Kyk_EGYTNTNM = kyk.EGYTNTNM;
        view.Kyk_TRKTNTNM = kyk.TRKTNTNM;
        view.Kyk_KYKSKNMJDN = kyk.KYKSKNMJDN;
        view.Kyk_NTINCHK_DS = kyk.NTINCHK_DS;
        view.Kyk_HCHSKNMJDN = kyk.HCHSKNMJDN;

        // 入力契約Noと最新契約Noが異なる場合
        if (!string.Equals(inputKykNo, latestKykNo, StringComparison.Ordinal))
        {
            //発注先名上段を上書き
            var hchsknmjdn = GetHCHSKNMJDN(db, inputKykNo);
            view.Kyk_HCHSKNMJDN =
                string.IsNullOrWhiteSpace(hchsknmjdn)
                    ? string.Empty
                    : hchsknmjdn;
        }

        return true;
    }

    /// <summary>
    /// 契約情報の取得
    /// </summary>
    /// <param name="db">Dbアクセス</param>
    /// <param name="kykNo">契約No</param>
    /// <returns>契約情報</returns>
    private static KeiyakuInfoDto? GetKeiyakutInfo(DbAccess db, string kykNo)
    {
        const string sql = @"
        SELECT
            SHDNCARNM,
            KATA,
            EGYTNTBUCD,
            EGYTNTNM,
            TRKTNTNM,
            KYKSKNMJDN,
            NTINCHK_DS,
            HCHSKNMJDN
        FROM JGSVFRMCHKKYK
        WHERE LSKYKNO = TRY_CONVERT(decimal(7, 0), @KYKNO);
        ";

        return db.Query<KeiyakuInfoDto>(sql, new { KYKNO = kykNo }).FirstOrDefault();
    }

    /// <summary>
    /// 発注時上段を発注時契約Noをキーに取得する
    /// </summary>
    /// <param name="db">Dbアクセス</param>
    /// <param name="fockykno">発注時契約No</param>
    /// <returns>発注時上段に表示する内容</returns>
    private static string? GetHCHSKNMJDN(DbAccess db, string fockykno)
    {
        const string sql =
            @"
            SELECT
                TKML0001.KSHNMJODAN AS HCHSKNMJDN
            FROM TLTL2001
            LEFT JOIN TLTL2031 ON TLTL2001.LSKYKNO = TLTL2031.LSKYKNO
            LEFT JOIN TKML0001 ON TLTL2031.HCHSKCD = TKML0001.SIRSKCD
            WHERE TLTL2001.LSKYKNO = TRY_CONVERT(decimal(7, 0), @OrderKykNo);
            ";

        //※ 0件なら null
        return db.ExecuteScalar<string?>(sql, new { OrderKykNo = fockykno });
    }

    /// <summary>
    /// 契約NoをキーにTLTL2001のデータ取得する
    /// </summary>
    /// <param name="db">Dbアクセス</param>
    /// <param name="kykNo">発注時契約No</param>
    /// <returns>発注時上段に表示する内容</returns>
    public int? GetTLTL2001(DbAccess db, string kykNo)
    {
        return db.ExecuteScalar<int>(
            @"
                SELECT COUNT(1)
                FROM TLTL2001
                WHERE LSKYKNO = TRY_CONVERT(decimal(7, 0), @KykNo);
                ", new { KykNo = kykNo });
    }

    /// <summary>
    /// 0/1フラグ（文字列）を正規化する
    /// </summary>
    /// <param name="value">元の値</param>
    /// <returns>"1" の場合のみ "1"、それ以外は "0"</returns>
    private static string Normalize01(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "0";
        }

        if (value.Trim() == "1")
        {
            return "1";
        }

        return "0";
    }

    /// <summary>
    /// 2項目のフラグが両方 "1" の場合に、両方 "0" へ正規化する
    /// </summary>
    /// <param name="left">左フラグ値</param>
    /// <param name="right">右フラグ値</param>
    private static void ChangeFlagValue(ref string left, ref string right)
    {
        if (left == "1" && right == "1")
        {
            left = "0";
            right = "0";
        }
    }

    /// <summary>
    /// 複数フラグのうち "1" が2つ以上存在する場合、全てを "0" に正規化する
    /// </summary>
    /// <param name="flags">フラグ名と値（"0" / "1"）のDictionary</param>
    private static void ChangeFlagValue(Dictionary<string, string> flags)
    {
        int onCount = 0;
        foreach (var v in flags.Values)
        {
            if (v == "1")
            {
                onCount++;
                if (onCount >= 2)
                {
                    break;
                }
            }
        }

        if (onCount >= 2)
        {
            var keys = flags.Keys.ToList();
            foreach (var k in keys)
            {
                flags[k] = "0";
            }
        }
    }

    /// <summary>
    /// "〒"を削除する
    /// </summary>
    /// <param name="value">郵便番号の項目値</param>
    /// <returns>"〒"を削除した文字列</returns>
    private static string? RemovePostalMark(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        return value.Replace("〒", string.Empty).Trim();
    }

    /// <summary>
    /// 正しい使用本拠の位置（FCOBSEUSECRT）が入力されている場合の使用本拠関連項目を正規化する
    /// </summary>
    /// <param name="useCrt">
    /// 正しい使用本拠の位置（OCR取込値）
    /// </param>
    /// <param name="use1">
    /// 使用本拠の位置1（郵便番号）
    /// FCOBSEUSECRT が入力されている場合は空
    /// </param>
    /// <param name="use2">
    /// 使用本拠の位置2
    /// FCOBSEUSECRT の先頭 200 文字が設定
    /// </param>
    /// <param name="use3">
    /// 使用本拠の位置3
    /// FCOBSEUSECRT の 201～400 文字目が設定
    /// </param>
    private static void NormalizeAddress(
        string? useCrt,
        ref string? use1,
        ref string? use2,
        ref string? use3)
    {
        // 正しい使用本拠の位置が未入力の場合は何もしない
        if (string.IsNullOrWhiteSpace(useCrt))
        {
            return;
        }

        var text = useCrt.Trim();

        // 郵便番号は空にする
        use1 = string.Empty;

        // 使用本拠の位置2 / 3 を初期化
        use2 = null;
        use3 = null;

        // Shift_JIS でバイト配列化
        var enc = Encoding.GetEncoding("shift_jis");
        byte[] bytes = enc.GetBytes(text);

        if (bytes.Length <= 200)
        {
            use2 = text;
            return;
        }

        // 200バイト目まで
        int len1 = GetSafeByteLength(bytes, enc, 0, 200);
        use2 = enc.GetString(bytes, 0, len1);

        // 残り（最大200バイト）
        int remain = bytes.Length - len1;
        if (remain > 0)
        {
            int len2 = GetSafeByteLength(bytes, enc, len1, Math.Min(200, remain));
            use3 = enc.GetString(bytes, len1, len2);
        }
    }

    private static int GetSafeByteLength(
                        byte[] bytes,
                        Encoding enc,
                        int start,
                        int maxLength)
    {
        int length = maxLength;

        // 文字境界を壊していないか確認
        while (length > 0)
        {
            try
            {
                enc.GetString(bytes, start, length);
                return length;
            }
            catch (ArgumentException)
            {
                // 文字途中で切れている → 1バイト戻す
                length--;
            }
        }

        return 0;
    }

    /// <summary>
    /// 変更フラグを判定する
    /// </summary>
    /// <param name="flag">変更フラグの値</param>
    /// <returns>
    /// 変更ありの場合は true、それ以外は false
    /// </returns>
    private static bool IsChanged(byte flag)
    {
        if (flag == 1)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// 確認テーブルの変更フラグ（…CHG）を基に、
    /// 画面表示用の「修正済み表示フラグ」をセットする
    /// </summary>
    /// <param name="view">確認テーブル（JGSTFRMCSVOCRCHK）から取得した表示用 ViewModel</param>
    private static void SetChangedFlags(FrameSheetCheckViewModel view)
    {
        view.Changed = new FrameSheetCheckViewModel.FrameSheetCheckChangedFlags
        {
            FOCKYKNO = IsChanged(view.FOCKYKNOCHG),
            FOCFRMNO = IsChanged(view.FOCFRMNOCHG),
            FOCFRMSERNO = IsChanged(view.FOCFRMSERNOCHG),
            FOCSTRTNT = IsChanged(view.FOCSTRTNTCHG),
            FOCSYUJI = IsChanged(view.FOCSYUJICHG),
            FOCSYUJO = IsChanged(view.FOCSYUJOCHG),
            FOCTRKYTI = IsChanged(view.FOCTRKYTICHG),
            FOCNSYYTI = IsChanged(view.FOCNSYYTICHG),
            FOCBSEUSE0 = IsChanged(view.FOCBSEUSE0CHG),
            FOCBSEUSE1 = IsChanged(view.FOCBSEUSE1CHG),
            FOCBSEUSE2 = IsChanged(view.FOCBSEUSE2CHG),
            FOCDOCARV = IsChanged(view.FOCDOCARVCHG),
        };
    }

    /// <summary>
    /// 文字列に差があるか判定を行う
    /// </summary>
    private static bool IsDifferentString(string? left, string? right)
    {
        var a = string.IsNullOrWhiteSpace(left) ? string.Empty : left.Trim();
        var b = string.IsNullOrWhiteSpace(right) ? string.Empty : right.Trim();
        return a != b;
    }

    /// <summary>
    /// 0/1フラグ文字列の差の判定を行う（NULL/空/空白は0扱い）
    /// </summary>
    private static bool IsDifferentFlag01(string? left, string? right)
    {
        var a = Normalize01(left);
        var b = Normalize01(right);
        return a != b;
    }

    /// <summary>
    /// 取込テーブル基準値（正規化後）と保存値を比較し、…CHG（0/1）を更新する
    /// </summary>
    /// <param name="after">更新対象（画面入力→保存値）</param>
    /// <param name="before">取込テーブル基準値（OCR→正規化→SaveModel）</param>
    private static void UpdateChangedFlags(FrameSheetCheckSaveModel after, FrameSheetCheckSaveModel before)
    {
        //契約No（発注時）
        after.FOCKYKNOCHG = IsDifferentString(after.FOCKYKNO, before.FOCKYKNO) ? 1 : 0;

        //車台番号
        after.FOCFRMNOCHG = IsDifferentString(after.FOCFRMNO, before.FOCFRMNO) ? 1 : 0;

        //車両特定番号
        after.FOCFRMSERNOCHG = IsDifferentString(after.FOCFRMSERNO, before.FOCFRMSERNO) ? 1 : 0;

        //販売店担当者
        after.FOCSTRTNTCHG = IsDifferentString(after.FOCSTRTNT, before.FOCSTRTNT) ? 1 : 0;

        //自家用／営業用（2項目をまとめて1フラグ）
        bool diffSyuj = IsDifferentFlag01(after.FOCSYUJI, before.FOCSYUJI);
        bool diffSyue = IsDifferentFlag01(after.FOCSYUEI, before.FOCSYUEI);
        after.FOCSYUJICHG = (diffSyuj || diffSyue) ? 1 : 0;

        //種類（9項目のどれか差分があれば1）
        bool diffKind =
            IsDifferentFlag01(after.FOCSYUJO, before.FOCSYUJO) ||
            IsDifferentFlag01(after.FOCSYU4NO, before.FOCSYU4NO) ||
            IsDifferentFlag01(after.FOCSYUKEI, before.FOCSYUKEI) ||
            IsDifferentFlag01(after.FOCSYU2TO, before.FOCSYU2TO) ||
            IsDifferentFlag01(after.FOCSYU2TU, before.FOCSYU2TU) ||
            IsDifferentFlag01(after.FOCSYU8TO, before.FOCSYU8TO) ||
            IsDifferentFlag01(after.FOCSYUKND, before.FOCSYUKND) ||
            IsDifferentFlag01(after.FOCSYUSPL, before.FOCSYUSPL) ||
            IsDifferentFlag01(after.FOCSYUSHR, before.FOCSYUSHR);

        after.FOCSYUJOCHG = diffKind ? 1 : 0;

        //登録予定日
        after.FOCTRKYTICHG = IsDifferentString(after.FOCTRKYTI, before.FOCTRKYTI) ? 1 : 0;
        //納車予定日
        after.FOCNSYYTICHG = IsDifferentString(after.FOCNSYYTI, before.FOCNSYYTI) ? 1 : 0;

        //都道府県
        after.FOCBSEUSE0CHG = IsDifferentString(after.FOCBSEUSE0, before.FOCBSEUSE0) ? 1 : 0;
        //郵便番号
        after.FOCBSEUSE1CHG = IsDifferentString(after.FOCBSEUSE1, before.FOCBSEUSE1) ? 1 : 0;
        //使用本拠の位置
        after.FOCBSEUSE2CHG = IsDifferentString(after.FOCBSEUSE2, before.FOCBSEUSE2) ||
                              IsDifferentString(after.FOCBSEUSE3, before.FOCBSEUSE3)
                              ? 1 : 0;

        // 登録書類必着日
        after.FOCDOCARVCHG = IsDifferentString(after.FOCDOCARV, before.FOCDOCARV) ? 1 : 0;
    }

    /// <summary>
    /// 確認テーブルへINSERTする
    /// </summary>
    /// <param name="db">DbAccessオブジェクト</param>
    /// <param name="m">保存モデル（INSERT/UPDATE共通）</param>
    private static void InsertChk(DbAccess db, FrameSheetCheckSaveModel m)
    {
        db.Execute(
            @"
            INSERT INTO dbo.JGSTFRMCSVOCRCHK
            (
                FOCFCOID,
                FOCIMPFILENM,
                FOCIMPRCDCNT,
                FOCFILENM,
                FOCFORMNM,
                FOCSTRNM1,
                FOCSTRNM2,
                FOCSTROFSNM,
                FOCSTRPSTNO,
                FOCSTRADD1,
                FOCSTRADD2,
                FOCSTRTEL,
                FOCKYKNO,
                FOCCARNM,
                FOCKATA,
                FOCBUNM,
                FOCTNTSTFNM,
                FOCKKYKNM1,
                FOCKKYKNM2,
                FOCOSS,
                FOCSTRTNT,
                FOCSTREML,
                FOCENTDAY,
                FOCSYUJI,
                FOCSYUEI,
                FOCSYUJO,
                FOCSYU4NO,
                FOCSYUKEI,
                FOCSYU2TO,
                FOCSYU2TU,
                FOCSYU8TO,
                FOCSYUKND,
                FOCSYUSPL,
                FOCSYUSHR,
                FOCSYUYOT,
                FOCFRMNO,
                FOCFRMSERNO,
                FOCTRKYTI,
                FOCNSYYTI,
                FOCBSEUSE0,
                FOCBSEUSE1,
                FOCBSEUSE2,
                FOCBSEUSE3,
                FOCBSEUSECRT,
                FOCDOCARV,
                FOCAMPM,
                FOCBIKO,
                FOCJCHSUBNO,
                FOCAM,
                FOCPM,
                FOCSYUNM1,
                FOCSYUNM2,
                FOCENTDAYREAD,
                FOCOCRDAY,
                FOCOCRDTTM,
                FOCIMPDT,
                FOCTMTNSY,
                FOCTMTPUP,
                FOCKYKNOCNF,
                FOCISLAND,
                FOCDOCARVCNF,
                FOCMEMO01,
                FOCMEMO02,
                FOCMEMO03,
                FOCMEMO04,
                FOCSTS,
                FOCTENMATSU,
                FOCKYKNOCHG,
                FOCFRMNOCHG,
                FOCFRMSERNOCHG,
                FOCSTRTNTCHG,
                FOCSYUJICHG,
                FOCSYUJOCHG,
                FOCTRKYTICHG,
                FOCNSYYTICHG,
                FOCBSEUSE0CHG,
                FOCBSEUSE1CHG,
                FOCBSEUSE2CHG,
                FOCDOCARVCHG,
                FOCCNFDATE,
                FOCCNFUSR,
                FOCINSDATE,
                FOCINSUSR,
                FOCUPDDATE,
                FOCUPDUSR
            )
            VALUES
            (
                @FOCFCOID,
                @FOCIMPFILENM,
                @FOCIMPRCDCNT,
                @FOCFILENM,
                @FOCFORMNM,
                @FOCSTRNM1,
                @FOCSTRNM2,
                @FOCSTROFSNM,
                @FOCSTRPSTNO,
                @FOCSTRADD1,
                @FOCSTRADD2,
                @FOCSTRTEL,
                @FOCKYKNO,
                @FOCCARNM,
                @FOCKATA,
                @FOCBUNM,
                @FOCTNTSTFNM,
                @FOCKKYKNM1,
                @FOCKKYKNM2,
                @FOCOSS,
                @FOCSTRTNT,
                @FOCSTREML,
                @FOCENTDAY,
                @FOCSYUJI,
                @FOCSYUEI,
                @FOCSYUJO,
                @FOCSYU4NO,
                @FOCSYUKEI,
                @FOCSYU2TO,
                @FOCSYU2TU,
                @FOCSYU8TO,
                @FOCSYUKND,
                @FOCSYUSPL,
                @FOCSYUSHR,
                @FOCSYUYOT,
                @FOCFRMNO,
                @FOCFRMSERNO,
                @FOCTRKYTI,
                @FOCNSYYTI,
                @FOCBSEUSE0,
                @FOCBSEUSE1,
                @FOCBSEUSE2,
                @FOCBSEUSE3,
                @FOCBSEUSECRT,
                @FOCDOCARV,
                @FOCAMPM,
                @FOCBIKO,
                @FOCJCHSUBNO,
                @FOCAM,
                @FOCPM,
                @FOCSYUNM1,
                @FOCSYUNM2,
                @FOCENTDAYREAD,
                @FOCOCRDAY,
                @FOCOCRDTTM,
                @FOCIMPDT,
                @FOCTMTNSY,
                @FOCTMTPUP,
                @FOCKYKNOCNF,
                @FOCISLAND,
                @FOCDOCARVCNF,
                @FOCMEMO01,
                @FOCMEMO02,
                @FOCMEMO03,
                @FOCMEMO04,
                @FOCSTS,
                @FOCTENMATSU,
                @FOCKYKNOCHG,
                @FOCFRMNOCHG,
                @FOCFRMSERNOCHG,
                @FOCSTRTNTCHG,
                @FOCSYUJICHG,
                @FOCSYUJOCHG,
                @FOCTRKYTICHG,
                @FOCNSYYTICHG,
                @FOCBSEUSE0CHG,
                @FOCBSEUSE1CHG,
                @FOCBSEUSE2CHG,
                @FOCDOCARVCHG,
                @FOCCNFDATE,
                @FOCCNFUSR,
                @FOCINSDATE,
                @FOCINSUSR,
                @FOCUPDDATE,
                @FOCUPDUSR
            );
            ", m);
    }

    /// <summary>
    /// 確認テーブル（JGSTFRMCSVOCRCHK）を更新する
    /// </summary>
    private void UpdateChk(DbAccess db, FrameSheetCheckSaveModel save)
    {
        var now = DateTime.Now;
        var param = new DynamicParameters(save);
        param.Add("OldUpdDate", save.FOCUPDDATE); //更新キーとして元の更新日時をセット
        //更新キーとして元の更新日時をセット
        param.Add("OldUpdDate", save.OldUpdDate);

        var rows = db.Execute(
            @"
            UPDATE dbo.JGSTFRMCSVOCRCHK
            SET
                -- ===== 販売店・契約情報 =====
                --FOCSTRNM1      = @FOCSTRNM1,
                FOCKYKNO       = @FOCKYKNO,
                --FOCCARNM       = @FOCCARNM,
                --FOCKATA        = @FOCKATA,
                --FOCBUNM        = @FOCBUNM,
                --FOCTNTSTFNM    = @FOCTNTSTFNM,
                --FOCKKYKNM1     = @FOCKKYKNM1,
                FOCSTRTNT      = @FOCSTRTNT,
                -- ===== 自動車種別（0/1）=====
                FOCSYUJI       = @FOCSYUJI,
                FOCSYUEI       = @FOCSYUEI,
                FOCSYUJO       = @FOCSYUJO,
                FOCSYU4NO      = @FOCSYU4NO,
                FOCSYUKEI      = @FOCSYUKEI,
                FOCSYU2TO      = @FOCSYU2TO,
                FOCSYU2TU      = @FOCSYU2TU,
                FOCSYU8TO      = @FOCSYU8TO,
                FOCSYUKND      = @FOCSYUKND,
                FOCSYUSPL      = @FOCSYUSPL,
                FOCSYUSHR      = @FOCSYUSHR,
                FOCSYUYOT      = @FOCSYUYOT,
                -- ===== 車両・登録情報 =====
                FOCFRMNO       = @FOCFRMNO,
                FOCFRMSERNO    = @FOCFRMSERNO,
                FOCTRKYTI      = @FOCTRKYTI,
                FOCNSYYTI      = @FOCNSYYTI,
                FOCBSEUSE0     = @FOCBSEUSE0,
                FOCBSEUSE1     = @FOCBSEUSE1,
                FOCBSEUSE2     = @FOCBSEUSE2,
                FOCBSEUSE3     = @FOCBSEUSE3,
                FOCDOCARV      = @FOCDOCARV,
                FOCAM          = @FOCAM,
                FOCPM          = @FOCPM,
                FOCTMTNSY      = @FOCTMTNSY,
                FOCTMTPUP      = @FOCTMTPUP,
                FOCKYKNOCNF    = @FOCKYKNOCNF,
                FOCISLAND      = @FOCISLAND,
                FOCDOCARVCNF   = @FOCDOCARVCNF,
                FOCMEMO01      = @FOCMEMO01,
                FOCMEMO02      = @FOCMEMO02,
                FOCMEMO03      = @FOCMEMO03,
                FOCMEMO04      = @FOCMEMO04,

                -- ===== ステータス =====
                FOCSTS         = @FOCSTS,
                FOCTENMATSU    = @FOCTENMATSU,              

                -- ===== 変更フラグ（CHG）=====
                FOCKYKNOCHG     = @FOCKYKNOCHG,
                FOCFRMNOCHG     = @FOCFRMNOCHG,
                FOCFRMSERNOCHG  = @FOCFRMSERNOCHG,
                FOCSTRTNTCHG    = @FOCSTRTNTCHG,
                FOCSYUJICHG     = @FOCSYUJICHG,
                FOCSYUJOCHG     = @FOCSYUJOCHG,
                FOCTRKYTICHG    = @FOCTRKYTICHG,
                FOCNSYYTICHG    = @FOCNSYYTICHG,
                FOCBSEUSE0CHG   = @FOCBSEUSE0CHG,
                FOCBSEUSE1CHG   = @FOCBSEUSE1CHG,
                FOCBSEUSE2CHG   = @FOCBSEUSE2CHG,
                FOCDOCARVCHG    = @FOCDOCARVCHG,
                FOCCNFDATE      = @FOCCNFDATE,
                FOCCNFUSR       = @FOCCNFUSR,
                FOCUPDDATE      = @FOCUPDDATE,
                FOCUPDUSR       = @FOCUPDUSR
            WHERE
                FOCFCOID = @FOCFCOID
                AND FOCUPDDATE = @OldUpdDate;
            ",
            param);

        if (rows == 0)
        {
            // 排他エラー
            throw new InvalidOperationException(
                "他のユーザーにより更新されているため、更新が行えませんでした。再読み込みしてください。");
        }
    }

    /// <summary>
    /// OCR取込テーブルのデータを取得
    /// </summary>
    /// <param name="db">DbAccessオブジェクト</param>
    /// <param name="fcoid">取込ID</param>
    /// <returns>OCR取込テーブルの1行</returns>
    private FrameSheetOcrRow LoadOcr(DbAccess db, int fcoid)
    {
        var ocr = db.Query<FrameSheetOcrRow>(
                SelectOcrSql,
                new { FCOID = fcoid })
            .FirstOrDefault();

        if (ocr == null)
        {
            throw new InvalidOperationException($"OCR取込データが存在しません。FCOID={fcoid}");
        }

        return ocr;
    }

    /// <summary>
    /// OCR取込テーブルから確認テーブルへINSERT
    /// </summary>
    /// <param name="db">DbAccessオブジェクト</param>
    /// <param name="fcoid">取込ID</param>
    private void InsertFromOcr(DbAccess db, int fcoid)
    {
        var ocr = LoadOcr(db, fcoid);
        var save = CreateSaveModelFromOcr(ocr);

        var now = DateTime.Now;
        save.FOCTENMATSU = 0;       //顛末
        save.FOCSTS = 9;            //9:初期Insert
        save.FOCCNFDATE = null;     //確定日時
        save.FOCCNFUSR = null;      //確定者
        save.FOCINSDATE = now;      //登録日時
        save.FOCINSUSR = _userId;   //登録者
        save.FOCUPDDATE = now;      //更新日時
        save.FOCUPDUSR = _userId;   //更新者

        //確認テーブルにINSERT
        InsertChk(db, save);
    }

    /// <summary>
    /// 取込テーブルから確認テーブルへINSERTするために整形を行う
    /// </summary>
    /// <param name="ocr">車台番号連絡票CSV取込テーブル（OCR）の行</param>
    /// <returns>車台番号連絡票CSV取込テーブル確認（OCR)への保存用モデル</returns>
    private FrameSheetCheckSaveModel CreateSaveModelFromOcr(FrameSheetOcrRow ocr)
    {
        // 正規化（自家用/営業用）
        var syuji = Normalize01(ocr.FCOSYUJI);
        var syuei = Normalize01(ocr.FCOSYUEI);
        ChangeFlagValue(ref syuji, ref syuei);

        // 正規化（種類9項目：複数1なら全部0）
        var kind = new Dictionary<string, string>
        {
            ["FOCSYUJO"] = Normalize01(ocr.FCOSYUJO),
            ["FOCSYU4NO"] = Normalize01(ocr.FCOSYU4NO),
            ["FOCSYUKEI"] = Normalize01(ocr.FCOSYUKEI),
            ["FOCSYU2TO"] = Normalize01(ocr.FCOSYU2TO),
            ["FOCSYU2TU"] = Normalize01(ocr.FCOSYU2TU),
            ["FOCSYU8TO"] = Normalize01(ocr.FCOSYU8TO),
            ["FOCSYUSPL"] = Normalize01(ocr.FCOSYUSPL),
            ["FOCSYUSHR"] = Normalize01(ocr.FCOSYUSHR),
            ["FOCSYUKND"] = Normalize01(ocr.FCOSYUKND),
        };
        ChangeFlagValue(kind);

        // AM/PM
        var am = Normalize01(ocr.FCOAM);
        var pm = Normalize01(ocr.FCOPM);
        ChangeFlagValue(ref am, ref pm);

        // 使用の本拠の位置
        var bseUse1 = ocr.FCOBSEUSE1;
        var bseUse2 = ocr.FCOBSEUSE2;
        var bseUse3 = ocr.FCOBSEUSE3;
        NormalizeAddress(
            ocr.FCOBSEUSECRT,
            ref bseUse1,
            ref bseUse2,
            ref bseUse3);

        // 「〒」削除
        bseUse1 = RemovePostalMark(bseUse1);

        return new FrameSheetCheckSaveModel
        {
            //FCO → FOC マッピング
            FOCFCOID = ocr.FCOID,
            FOCIMPFILENM = ocr.FCOIMPFILENM,
            FOCIMPRCDCNT = ocr.FCOIMPRCDCNT,
            FOCFILENM = ocr.FCOFILENM,
            FOCFORMNM = ocr.FCOFORMNM,
            FOCSTRNM1 = ocr.FCOSTRNM1,
            FOCSTRNM2 = ocr.FCOSTRNM2,
            FOCSTROFSNM = ocr.FCOSTROFSNM,
            FOCSTRPSTNO = ocr.FCOSTRPSTNO,
            FOCSTRADD1 = ocr.FCOSTRADD1,
            FOCSTRADD2 = ocr.FCOSTRADD2,
            FOCSTRTEL = ocr.FCOSTRTEL,
            FOCKYKNO = ocr.FCOKYKNO,
            FOCCARNM = ocr.FCOCARNM,
            FOCKATA = ocr.FCOKATA,
            FOCBUNM = ocr.FCOBUNM,
            FOCTNTSTFNM = ocr.FCOTNTSTFNM,
            FOCKKYKNM1 = ocr.FCOKKYKNM1,
            FOCKKYKNM2 = ocr.FCOKKYKNM2,
            FOCOSS = ocr.FCOOSS,
            FOCSTRTNT = ocr.FCOSTRTNT,
            FOCSTREML = ocr.FCOSTREML,
            FOCENTDAY = ocr.FCOENTDAY,

            FOCSYUJI = syuji,
            FOCSYUEI = syuei,
            FOCSYUJO = kind["FOCSYUJO"],
            FOCSYU4NO = kind["FOCSYU4NO"],
            FOCSYUKEI = kind["FOCSYUKEI"],
            FOCSYU2TO = kind["FOCSYU2TO"],
            FOCSYU2TU = kind["FOCSYU2TU"],
            FOCSYU8TO = kind["FOCSYU8TO"],
            FOCSYUKND = kind["FOCSYUKND"],
            FOCSYUSPL = kind["FOCSYUSPL"],
            FOCSYUSHR = kind["FOCSYUSHR"],

            FOCSYUYOT = ocr.FCOSYUYOT,
            FOCFRMNO = ocr.FCOFRMNO,
            FOCFRMSERNO = ocr.FCOFRMSERNO,
            FOCTRKYTI = ocr.FCOTRKYTI,
            FOCNSYYTI = ocr.FCONSYYTI,

            FOCBSEUSE0 = ocr.FCOBSEUSE0,
            FOCBSEUSE1 = bseUse1,
            FOCBSEUSE2 = bseUse2,
            FOCBSEUSE3 = bseUse3,
            FOCBSEUSECRT = ocr.FCOBSEUSECRT,

            FOCDOCARV = ocr.FCODOCARV,
            FOCAMPM = ocr.FCOAMPM,
            FOCBIKO = ocr.FCOBIKO,
            FOCJCHSUBNO = ocr.FCOJCHSUBNO,
            FOCAM = am,
            FOCPM = pm,
            FOCSYUNM1 = ocr.FCOSYUNM1,
            FOCSYUNM2 = ocr.FCOSYUNM2,
            FOCENTDAYREAD = ocr.FCOENTDAYREAD,
            FOCOCRDAY = ocr.FCOOCRDAY,
            FOCOCRDTTM = ocr.FCOOCRDTTM,
            FOCIMPDT = ocr.FCOIMPDT,

            // 初期値
            FOCSTS = 0,
            FOCKYKNOCHG = 0,
            FOCFRMNOCHG = 0,
            FOCFRMSERNOCHG = 0,
            FOCSTRTNTCHG = 0,
            FOCSYUJICHG = 0,
            FOCSYUJOCHG = 0,
            FOCTRKYTICHG = 0,
            FOCNSYYTICHG = 0,
            FOCBSEUSE0CHG = 0,
            FOCBSEUSE1CHG = 0,
            FOCBSEUSE2CHG = 0,
            FOCDOCARVCHG = 0,

            FOCCNFDATE = null,      //確定日時
            FOCCNFUSR = null,       //確定者
            FOCINSDATE = null,      //登録日時
            FOCINSUSR = null,       //登録者
            FOCUPDDATE = null,      //更新日時
            FOCUPDUSR = null,       //更新者
        };
    }

    /// <summary>
    /// 最新の契約番号を取得
    /// </summary>
    /// <param name="db">Dbアクセス</param>
    /// <param name="startNo">発注時契約No</param>
    /// <returns>契約番号</returns>
    private string GetLatestKeiyakuNo(DbAccess db, string startNo)
    {

        string current = startNo;   // 最初に指定された契約番号
        HashSet<string> visited = new HashSet<string>(); // 無限ループ対策

        while (true)
        {
            // 無限ループ防止
            if (visited.Contains(current))
            {
                break;
            }
            visited.Add(current);

            var next = GetAfterNo(db, current); // SQL を呼び出して差替え後番号を取得

            if (string.IsNullOrEmpty(next))
            {
                break; // 差替え後が無い → これが最終
            }
            current = next; // 次へ進む
        }

        return current;

    }

    /// <summary>
    /// 差替後契約Noを取得する処理
    /// </summary>
    /// <param name="db">Dbアクセス</param>
    /// <param name="befNo">差替元の契約No</param>
    /// <returns>差替後契約No</returns>
    private string? GetAfterNo(DbAccess db, string befNo)
    {

        return db.Query<string>(
            @"
            SELECT JSEAFTNO
            FROM JMSTJCHSKE
            WHERE JSEBEFNO = TRY_CONVERT(decimal(7,0), @befNo);
            ",
            new { befNo })
            .FirstOrDefault();

    }

    private IReadOnlyList<JGSVNAMReader.NameItem> LoadComboItems(DbAccess db)
    {
        var reader = new JGSVNAMReader(db);
        return reader.GetNames("FC01");
    }

    /// <summary>契約情報取得用</summary>
    internal sealed class KeiyakuInfoDto
    {
        /// <summary>契約No</summary>
        public string? LSKYNO { get; set; }

        /// <summary>車名</summary>
        public string? SHDNCARNM { get; set; }

        /// <summary>型式</summary>
        public string? KATA { get; set; }

        /// <summary>担当部署コード</summary>
        public string? EGYTNTBUCD { get; set; }

        /// <summary>担当スタッフ名</summary>
        public string? EGYTNTNM { get; set; }

        /// <summary>担当チーム</summary>
        public string? TRKTNTNM { get; set; }

        /// <summary>契約先名上段</summary>
        public string? KYKSKNMJDN { get; set; }

        /// <summary>捺印書類送付先確定情報</summary>
        public string? NTINCHK_DS { get; set; }

        /// <summary>発注先名上段（発注時）</summary>
        public string? HCHSKNMJDN { get; set; }
    }
}
