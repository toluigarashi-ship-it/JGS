using System;
using System.Linq;
using System.Windows.Forms;
using Common.Db;
using DesktopApp.DesktopCommon.ControlManager;
using DesktopApp.DesktopCommon.DataAccess;
using DesktopApp.FrameSheetCheck;
using GrapeCity.Win.MultiRow;
using Microsoft.VisualBasic.ApplicationServices;
using MultiRowTemplateCreate.Templates;

namespace DesktopApp.FrameSheetList;

/// <summary>
/// 車台番号連絡表取込一覧フォーム
/// </summary>
public partial class FrameSheetListForm : Form
{
    #region プライベート変数

    /// <summary>ロジッククラス</summary>
    private readonly FrameSheetListLogic _logic;
    private readonly FrameSheetListViewModel _viewModel = new FrameSheetListViewModel();

    private readonly BindingSource _condBindingSource = new BindingSource();

    /// <summary>接続文字列</summary>
    private readonly string _connectionString;

    /// <summary>ログインID</summary>
    private readonly string _userId;

    ///// <summary>ロード済フラグ</summary>
    //private bool _loaded;

    /// <summary>初回表示フラグ</summary>
    private bool _isFirstShown;

    /// <summary>gcMultiRow1の表示行件数（KensuLbl反映済みの値）</summary>
    private int _currentVisibleRowCount = -1;

    #endregion

    #region コンストラクタ

    /// <summary>
    /// 車台番号連絡表取込一覧フォームの実装
    /// </summary>
    public FrameSheetListForm()
    {
        InitializeComponent();

        //Configの設定取得
        _connectionString = DbConnection.GetSqlConnectionString();

        //ログインID
        if (DesktopApp.Properties.Settings.Default.SavedLoginId == null)
        {
            throw new ArgumentException("Settings.Default.SavedLoginIdの設定が不正です。");
        }
        _userId = DesktopApp.Properties.Settings.Default.SavedLoginId;

        // ---- MultiRowの設定 ----
        GcMultiRow1.Template = new FrameSheetListFormTemplate();
        HookHeaderFilterClosed();
        //常に行全体を選択する設定
        GcMultiRow1.ViewMode = GrapeCity.Win.MultiRow.ViewMode.Row;
        //カレントセルの点線描画を抑制
        GcMultiRow1.CurrentCellBorderLine = new Line(LineStyle.None, Color.Empty);

        //Logic生成
        _logic = new FrameSheetListLogic(_connectionString, _userId);

    }

    #endregion

    #region イベント

    /// <summary>
    /// 画面表示時のイベント
    /// コントロールに検索条件Modelをバインド→検索条件を初期化
    /// </summary>
    private void FrameSheetListForm_Load(object sender, EventArgs e)
    {
        //検索条件コントロールとモデルを結合
        SetupConditionBindings();

        //検索条件初期化（初回表示時のみ未確認ON）
        InitializeSearchConditionsForFirstDisplay();

        //MultiRowにデータソースバインド
        GcMultiRow1.DataSource = _viewModel.Items;

        //実行環境に合わせて背景色を設定
        ControlManager.AppEnvModeBackColor(this);

    }

    /// <summary>
    /// フォーム表示後のイベント
    /// </summary>
    private void FrameSheetListForm_Shown(object sender, EventArgs e)
    {
        if (this._isFirstShown)
        {
            return;
        }

        //レイアウト確定後の動作を保証するため、BeginInvokeで列幅調整を実行
        this.BeginInvoke(new System.Action(() =>
        {
            this.AdjustColumnWidths();
        }));

        this._isFirstShown = true;
        SearchExecute();
    }

    /// <summary>
    /// 検索ボタン押下時のイベント
    /// </summary>
    private void SearchBtn_Click(object sender, EventArgs e)
    {
        SearchExecute();
    }

    /// <summary>
    /// 検索条件クリアボタンの押下時イベント
    /// </summary>
    private void ClearBtn_Click(object sender, EventArgs e)
    {
        ClearSearchConditions();
    }

    /// <summary>
    /// セルダブルクリック時イベント
    /// 現状はポインタ位置や右クリックは関係ないのでCellMouseDoubleClickではなくこちらに実装
    /// </summary>
    private void GcMultiRow1_CellDoubleClick(object sender, CellEventArgs e)
    {
        // ヘッダ（RowIndex=-1）や無効クリックを除外
        if (e.RowIndex < 0 || e.CellIndex < 0)
        {
            return;
        }

        // ダブルクリックされたセルが行ヘッダセルならOK
        if (GcMultiRow1[e.RowIndex, e.CellIndex] is RowHeaderCell)
        {
            OpenFrameSheetCheckFormForRow(e.RowIndex);
        }
    }

    /// <summary>
    /// 閉じるボタンクリック時イベント
    /// </summary>
    private void CloseBtn_Click(object sender, EventArgs e)
    {
        this.Close();
    }
    #endregion

    #region Privateメソッド

    /// <summary>
    /// ViewModelの内容を画面に反映する
    /// </summary>
    private void BindViewModel()
    {
        // GcMultiRow への表示
        GcMultiRow1.DataSource = _viewModel.Items;

        // 件数表示
        KakuninMaeLbl.Text = _viewModel.Summary.CNT_KAKUNINMAE.ToString();
        IchijiHozonLbl.Text = _viewModel.Summary.CNT_ICHIZON.ToString();
        UpdateVisibleRowCountLabel(); //MultiRow表示中件数は画面の挙動で変動するので別処理
    }

    /// <summary>
    /// gcMultiRow1の現在表示行数を取得し、KensuLblに反映する
    /// </summary>
    private void UpdateVisibleRowCountLabel()
    {
        var visibleCount = 0;

        foreach (Row row in GcMultiRow1.Rows)
        {
            if (row.Visible)
            {
                visibleCount++;
            }
        }

        if (_currentVisibleRowCount == visibleCount)
        {
            return;
        }

        _currentVisibleRowCount = visibleCount;
        KensuLbl.Text = visibleCount.ToString();
    }

    /// <summary>
    /// 検索条件（ViewModel.Conditions）と画面コントロールをバインドする
    /// </summary>
    private void SetupConditionBindings()
    {
        // 2重登録防止（Shownが複数回呼ばれても安全に）
        if (_condBindingSource.DataSource is not null)
        {
            return;
        }

        _condBindingSource.DataSource = _viewModel.Conditions;

        // 文字列
        StrNmTxt.DataBindings.Add("Text", _condBindingSource, nameof(FrameSheetListSearchConditions.CondSTRNM), true, DataSourceUpdateMode.OnPropertyChanged);
        KkykNmTxt.DataBindings.Add("Text", _condBindingSource, nameof(FrameSheetListSearchConditions.CondKKYKNM), true, DataSourceUpdateMode.OnPropertyChanged);
        FrmSerNoTxt.DataBindings.Add("Text", _condBindingSource, nameof(FrameSheetListSearchConditions.CondFRMSERNO), true, DataSourceUpdateMode.OnPropertyChanged);
        FrmNoTxt.DataBindings.Add("Text", _condBindingSource, nameof(FrameSheetListSearchConditions.CondFRMNO), true, DataSourceUpdateMode.OnPropertyChanged);
        HchkykNoTxt.DataBindings.Add("Text", _condBindingSource, nameof(FrameSheetListSearchConditions.CondHCHKYKNO), true, DataSourceUpdateMode.OnPropertyChanged);

        // 日付
        ImpDtFrm.DataBindings.Add("Value", _condBindingSource, nameof(FrameSheetListSearchConditions.CondIMPDTFrom), true, DataSourceUpdateMode.OnPropertyChanged);
        ImpDtTo.DataBindings.Add("Value", _condBindingSource, nameof(FrameSheetListSearchConditions.CondIMPDTTo), true, DataSourceUpdateMode.OnPropertyChanged);

        // 種別
        CsvTyp1Chk.DataBindings.Add("Checked", _condBindingSource, nameof(FrameSheetListSearchConditions.CondCSVTYP_Normal), true, DataSourceUpdateMode.OnPropertyChanged);
        CsvTyp2Chk.DataBindings.Add("Checked", _condBindingSource, nameof(FrameSheetListSearchConditions.CondCSVTYP_Tmt), true, DataSourceUpdateMode.OnPropertyChanged);

        // ステータス
        Status99Chk.DataBindings.Add("Checked", _condBindingSource, nameof(FrameSheetListSearchConditions.CondSTS_Unregistered), true, DataSourceUpdateMode.OnPropertyChanged);
        Status1Chk.DataBindings.Add("Checked", _condBindingSource, nameof(FrameSheetListSearchConditions.CondSTS_Temporary), true, DataSourceUpdateMode.OnPropertyChanged);
        Status2Chk.DataBindings.Add("Checked", _condBindingSource, nameof(FrameSheetListSearchConditions.CondSTS_ResendRequest), true, DataSourceUpdateMode.OnPropertyChanged);
        Status0Chk.DataBindings.Add("Checked", _condBindingSource, nameof(FrameSheetListSearchConditions.CondSTS_Confirmed), true, DataSourceUpdateMode.OnPropertyChanged);

        // 担当UT
        TntUT0Chk.DataBindings.Add("Checked", _condBindingSource, nameof(FrameSheetListSearchConditions.CondTNTUT_Register), true, DataSourceUpdateMode.OnPropertyChanged);
        TntUT1Chk.DataBindings.Add("Checked", _condBindingSource, nameof(FrameSheetListSearchConditions.CondTNTUT_Document), true, DataSourceUpdateMode.OnPropertyChanged);
    }

    /// <summary>
    /// 検索ボタン・初回表示時用：検索処理の実行時の画面処理
    /// </summary>
    private async void SearchExecute()
    {
        try
        {
            UseWaitCursor = true;
            GcMultiRow1.Enabled = false;
            SearchBtn.Enabled = false;
            ClearBtn.Enabled = false;

            await _logic.SearchAsync(_viewModel);

            BindViewModel();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "一覧の取得に失敗しました。\r" +
                "message:" + ex.Message,
                "一覧取得エラー",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
        finally
        {
            GcMultiRow1.Enabled = true;
            UseWaitCursor = false;
            SearchBtn.Enabled = true;
            ClearBtn.Enabled = true;
            GcMultiRow1.ClearSelection();
        }
    }

    /// <summary>
    /// クリアボタン用：検索条件を初期化する（画面はバインディングで自動反映される）
    /// </summary>
    private void ClearSearchConditions()
    {
        _viewModel.Conditions.Clear();
    }

    /// <summary>
    /// 初回表示時の検索条件を初期化する（未確認のみON）
    /// </summary>
    private void InitializeSearchConditionsForFirstDisplay()
    {
        _viewModel.Conditions.InitializeForFirstDisplay();
    }

    /// <summary>
    /// MultiRowの表示幅に合わせて列幅を調整する（縦スクロールバーがある想定）
    /// 販売店名とお客様名で分け合う
    /// </summary>
    private void AdjustColumnWidths()
    {
        var viewWidth = this.GcMultiRow1.ClientSize.Width - SystemInformation.VerticalScrollBarWidth;
        if (viewWidth <= 0)
        {
            return;
        }

        if (this.GcMultiRow1.ColumnHeaders.Count == 0)
        {
            return;
        }

        var header = this.GcMultiRow1.ColumnHeaders[0];

        const string ExpandHeader1 = "StrNmHeader";
        const string ExpandHeader2 = "KkykNmHeader";

        if (!TryGetCellByName(header.Cells, ExpandHeader1, out var header1)
            || !TryGetCellByName(header.Cells, ExpandHeader2, out var header2))
        {
            return;
        }

        // 現在の列幅合計（ヘッダから読む）
        var columnsWidthTotal = 0;
        foreach (GrapeCity.Win.MultiRow.Cell cell in header.Cells)
        {
            columnsWidthTotal += cell.Width;
        }

        var extra = viewWidth - columnsWidthTotal;
        if (extra <= 0)
        {
            return;
        }

        var add1 = extra / 2;
        var add2 = extra - add1;

        // null許容で受け取っているのでnull許容抑止
        header1!.HorizontalResize(add1);
        header2!.HorizontalResize(add2);

        static bool TryGetCellByName(GrapeCity.Win.MultiRow.CellCollection cells, string name, out GrapeCity.Win.MultiRow.Cell? cell)
        {
            foreach (GrapeCity.Win.MultiRow.Cell c in cells)
            {
                if (string.Equals(c.Name, name, StringComparison.Ordinal))
                {
                    cell = c;
                    return true;
                }
            }

            cell = null;
            return false;
        }
    }

    /// <summary>
    /// ヘッダセルのフィルタ用ドロップダウンメニューのClosedイベントを購読する
    /// </summary>
    private void HookHeaderFilterClosed()
    {
        foreach (var section in this.GcMultiRow1.Template.ColumnHeaders)
        {
            foreach (var cell in section.Cells)
            {
                if (cell is GrapeCity.Win.MultiRow.ColumnHeaderCell headerCell &&
                    headerCell.DropDownContextMenuStrip is GrapeCity.Win.MultiRow.HeaderDropDownContextMenu ddcm)
                {
                    ddcm.Closed -= this.HeaderDropDownListClosed;
                    ddcm.Closed += this.HeaderDropDownListClosed;
                }
            }
        }
    }

    /// <summary>
    /// ヘッダのフィルタメニューが閉じられた後に表示件数ラベルを更新する
    /// </summary>
    /// <param name="sender">Closedイベントの送信元</param>
    /// <param name="e">イベント引数</param>
    private void HeaderDropDownListClosed(object? sender, EventArgs e)
    {
        UpdateVisibleRowCountLabel();
    }

    /// <summary>
    /// 現在表示中のgcMultiRow1データから、確認画面遷移に必要なキー一覧を生成する
    /// </summary>
    /// <returns>(Type, Id)のリスト</returns>
    private List<(int Type, int Id)> CreateVisibleKeyList()
    {
        var keyList = new List<(int Type, int Id)>();

        foreach (Row row in GcMultiRow1.Rows)
        {
            if (!row.Visible)
            {
                continue;
            }

            if (row.DataBoundItem is FrameSheetListRowViewModel item)
            {
                keyList.Add((item.CSVTYP, item.ID));
            }
        }

        return keyList;
    }

    /// <summary>
    /// 指定された行インデックスが、現在表示中キー一覧の何番目かを取得する
    /// </summary>
    /// <param name="rowIndex">gcMultiRow1上の行インデックス</param>
    /// <param name="keyIndex">表示中キー一覧のインデックス</param>
    /// <returns>取得可否</returns>
    private bool TryGetVisibleKeyIndex(int rowIndex, out int keyIndex)
    {
        keyIndex = -1;

        if (rowIndex < 0 || rowIndex >= GcMultiRow1.Rows.Count)
        {
            return false;
        }

        var visibleDataRowIndex = 0;

        for (var i = 0; i < GcMultiRow1.Rows.Count; i++)
        {
            var row = GcMultiRow1.Rows[i];

            if (!row.Visible || row.DataBoundItem is not FrameSheetListRowViewModel)
            {
                continue;
            }

            if (i == rowIndex)
            {
                keyIndex = visibleDataRowIndex;
                return true;
            }

            visibleDataRowIndex++;
        }

        return false;
    }

    /// <summary>
    /// 指定行を起点に確認画面を開く
    /// </summary>
    /// <param name="rowIndex">対象行インデックス</param>
    private void OpenFrameSheetCheckFormForRow(int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= GcMultiRow1.Rows.Count)
        {
            return;
        }

        var targetRow = GcMultiRow1.Rows[rowIndex];
        if (targetRow.DataBoundItem is not FrameSheetListRowViewModel item)
        {
            return;
        }

        var keyList = CreateVisibleKeyList();
        if (keyList.Count == 0)
        {
            return;
        }

        if (!TryGetVisibleKeyIndex(rowIndex, out var keyIndex))
        {
            return;
        }

        _viewModel.SelectedItem = item;

        using var checkForm = new FrameSheetCheckForm(keyList, keyIndex);
        checkForm.ShowDialog(this);
    }

    #endregion

}
