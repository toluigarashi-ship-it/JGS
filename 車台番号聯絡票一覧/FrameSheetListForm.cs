using System;
using System.Linq;
using System.Windows.Forms;
using Common.Db;
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

    /// <summary>
    /// gcMultiRow1の表示行件数（KensuLbl反映済みの値）
    /// </summary>
    private int _currentVisibleRowCount = -1;

    #endregion

    #region コンストラクタ

    /// <summary>
    /// 車台番号連絡表取込一覧フォームの実装
    /// </summary>
    public FrameSheetListForm()
    {
        InitializeComponent();
        gcMultiRow1.Template = new FrameSheetListFormTemplate();
        gcMultiRow1.Layout += GcMultiRow1_Layout;

        // ---- Configの設定取得 ----
        _connectionString = DbConnection.GetSqlConnectionString();
        //ログインID
        if (DesktopApp.Properties.Settings.Default.SavedLoginId == null)
        {
            throw new ArgumentException("Settings.Default.SavedLoginIdの設定が不正です。");
        }
        _userId = DesktopApp.Properties.Settings.Default.SavedLoginId;

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
        SetupConditionBindings();
        ClearSearchConditions();
        gcMultiRow1.DataSource = _viewModel;
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

    #endregion

    #region Privateメソッド

    /// <summary>
    /// ViewModelの内容を画面に反映する
    /// </summary>
    private void BindViewModel()
    {
        // GcMultiRow への表示
        // DataSourceで行ける構成ならこれが簡単（テンプレ側のDataField設定が前提）
        gcMultiRow1.DataSource = _viewModel.Items;

        // 件数表示
        KakuninMaeLbl.Text = _viewModel.Summary.CNT_KAKUNINMAE.ToString();
        IchijiHozonLbl.Text = _viewModel.Summary.CNT_ICHIZON.ToString();
        UpdateVisibleRowCountLabel();
    }

    /// <summary>
    /// MultiRowのレイアウト更新時に件数表示を再計算する
    /// （ヘッダーフィルター適用時の表示件数連動）
    /// </summary>
    private void GcMultiRow1_Layout(object? sender, LayoutEventArgs e)
    {
        UpdateVisibleRowCountLabel();
    }

    /// <summary>
    /// gcMultiRow1の現在表示行数を取得し、KensuLblに反映する
    /// </summary>
    private void UpdateVisibleRowCountLabel()
    {
        var visibleCount = 0;

        foreach (Row row in gcMultiRow1.Rows)
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
            gcMultiRow1.Enabled = false;
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
            gcMultiRow1.Enabled = true;
            UseWaitCursor = false;
            SearchBtn.Enabled = true;
            ClearBtn.Enabled = true;
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
    /// MultiRowの表示幅に合わせて列幅を調整する（縦スクロールバーがある想定）
    /// 販売店名とお客様名で分け合う
    /// </summary>
    private void AdjustColumnWidths()
    {
        var viewWidth = this.gcMultiRow1.ClientSize.Width - SystemInformation.VerticalScrollBarWidth;
        if (viewWidth <= 0)
        {
            return;
        }

        if (this.gcMultiRow1.ColumnHeaders.Count == 0)
        {
            return;
        }

        var header = this.gcMultiRow1.ColumnHeaders[0];

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

    #endregion

}
