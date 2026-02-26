using System.Text;
using Common.Db;
using Common.Logging;
using DesktopApp.DesktopCommon.ControlManager;
using DesktopApp.DesktopCommon.Conversion;
using DesktopApp.DesktopCommon.DataAccess;
using DesktopApp.FrameSheetCheck;
using GrapeCity.Win.Editors;
using NLog;
using PdfiumViewer;

namespace DesktopApp.FrameSheetCheck;

/// <summary>
/// 車台番号連絡票確認画面フォーム
/// </summary>
public partial class FrameSheetCheckForm : Form
{
    #region プライベート変数

    /// <summary>
    /// ロジッククラス
    /// </summary>
    private readonly FrameSheetCheckLogic _logic;

    /// <summary>
    /// 接続文字列
    /// </summary>
    private readonly string _connectionString;

    /// <summary>
    /// ログインID
    /// </summary>
    private readonly string _userId;

    /// <summary>
    /// 表示中データ
    /// </summary>
    private FrameSheetCheckViewModel? _currentView = null;

    /// <summary>
    /// PDF保存フォルダ
    /// </summary>
    private string _pdfFolder = string.Empty;

    /// <summary>
    /// 画面初期表示中フラグ
    /// </summary>
    private bool _isInitializing;

    /// <summary>
    /// チェックボックスのイベント再発生防止用
    /// </summary>
    private bool _suppressKykCheckEvent;

    // ErrorProviderのインスタンスを作成
    private ErrorProvider _errorProvider = new ErrorProvider();

    /// <summary>
    /// ダーティー判定用
    /// </summary>
    private FrameSheetCheckSaveModel? _initialSnapshot;

    #endregion

    #region コンストラクタ

    /// <summary>
    /// 車台番号連絡票確認画面フォームの実装
    /// </summary>
    public FrameSheetCheckForm()
    {

        InitializeComponent();

        // ---- NLogの設定 ----
        //NLog.configの読み込みと初期化
        LogManager.Setup().LoadConfigurationFromFile("NLog.config");
        Log.Initialize();

        Log.Info("車台番号連絡票確認画面　開始");

        // ---- Configの設定取得 ----
        _connectionString = DbConnection.GetSqlConnectionString();
        //ログインID
        if (DesktopApp.Properties.Settings.Default.SavedLoginId == null)
        {
            throw new ArgumentException("Settings.Default.SavedLoginIdの設定が不正です。");
        }
        _userId = DesktopApp.Properties.Settings.Default.SavedLoginId;
        //PDF保存フォルダ
        if (DesktopApp.Properties.Settings.Default.FrameSheetPdfFolder == null)
        {
            throw new ArgumentException("Settings.Default.FrameSheetPdfFolderの設定が不正です。");
        }
        _pdfFolder = DesktopApp.Properties.Settings.Default.FrameSheetPdfFolder;

        // ---- ロジッククラス設定 ----
        _logic = new FrameSheetCheckLogic(_connectionString, _userId);

    }

    #endregion

    #region 構造体

    /// <summary>
    /// 一時保存／確定
    /// </summary>
    internal enum SaveAction
    {
        /// <summary>一時保存</summary>
        Temporary,

        /// <summary>確定</summary>
        Confirm,
    }

    /// <summary>
    /// 表示メッセージの種類
    /// </summary>
    internal enum MsgType
    {
        /// <summary>通常</summary>
        Info,

        /// <summary>エラー</summary>
        Error,
    }

    #endregion

    #region プロパティ

    /// <summary>
    /// 取込ID
    /// フォーム起動前に設定される
    /// </summary>
    public int FCOID { get; set; }
    #endregion

    #region イベント

    /// <summary>
    /// フォームロード時の処理
    /// 画面描画後に初期表示データを取得し、画面項目へ反映する
    /// </summary>
    private void FrameSheetCheckForm_Load(object sender, EventArgs e)
    {

        this.SuspendLayout();
        try
        {
            //初期表示中フラグON
            _isInitializing = true;

            //画面初期値クリア
            ClearView();

            //データロード
            var view = _logic.LoadInitial(FCOID);
            if (view == null)
            {
                Log.Error($"対象データなし。取込ID：{FCOID}");
                return;
            }
            if (string.IsNullOrWhiteSpace(view.FOCFILENM))
            {
                Log.Error($"取込データにPDFファイル名が登録されていません。取込ID：{FCOID}");
                return;
            }
            string pdfFileNm = Path.Combine(_pdfFolder, view.FOCFILENM);
            if (!File.Exists(pdfFileNm))
            {
                Log.Error($"PDFファイルが見つかりません。ファイル：{pdfFileNm}");
                return;
            }

            //表示中データとして保持
            _currentView = view;

            //画面表示
            DispView(view);

            //PdfViewerの設定
            SetPdfViewerProperty(pdfFileNm);

            //エラーアイコンを点滅なしに設定する
            _errorProvider.BlinkStyle = ErrorBlinkStyle.NeverBlink;

        }
        catch (Exception ex)
        {
            SetExceptionLabel();
            Log.Error(ex, "画面表示中にエラーが発生しました。");
            Close();
        }
        finally
        {
            //初期表示中フラグOFF
            _isInitializing = false;
            this.ResumeLayout();
            this.Refresh();
        }

    }

    /// <summary>
    /// フォームクローズ前の処理
    /// </summary>
    private void FrameSheetCheckForm_FormClosing(object sender, FormClosingEventArgs e)
    {
        if (IsModified())
        {
            if (MessageBox.Show("画面の内容が変更されています。保存せずに閉じてもよろしいですか？", "確認", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
                == DialogResult.No)
            {
                e.Cancel = true;
            }
        }
    }

    /// <summary>
    /// フォームクローズ後の処理
    /// </summary>
    private void FrameSheetCheckForm_FormClosed(object sender, FormClosedEventArgs e)
    {
        Log.Info("車台番号連絡票確認画面　終了");
    }

    /// <summary>
    /// 画面サイズ変更用
    /// </summary>
    private void GcResize1_ControlResizing(object sender, GrapeCity.Win.Components.GcResizeEventArgs e)
    {
        gcResizePanel1.Location = new Point(gcContainer7.Width + gcContainer7.Location.X, gcResizePanel1.Location.Y);
        gcResizePanel1.Width = this.Width - gcContainer7.Width - gcContainer7.Location.X - 25;
    }

    /// <summary>
    /// 発注時契約No変更ボタン押下時の処理
    /// </summary>
    private void KyknoBtn_Click(object sender, EventArgs e)
    {
        //発注時契約Noを変更可にする
        ControlManager.SetEnabledState(HchukyknoTxt, true);
        if (_currentView != null)
        {
            HchukyknoTxt.BackColor =
                _currentView.Changed.FOCKYKNO
                ? Color.LightYellow : SystemColors.Window;
        }

        KyknoconfChk.Checked = false;
        HchukyknoTxt.TabStop = true;
        HchukyknoTxt.SelectAll();
        HchukyknoTxt.Focus();
    }

    /// <summary>
    /// 発注時契約No変更時の処理
    /// </summary>
    private void HchukyknoTxt_TextChanged(object sender, EventArgs e)
    {
        //確認チェックボックスをOFFにする
        KyknoconfChk.Checked = false;
    }

    /// <summary>
    /// 契約No確認チェックボックスのラベルクリック
    /// </summary>
    private void KyknoconfLbl_Click(object sender, EventArgs e)
    {
        KyknoconfChk.Checked = !KyknoconfChk.Checked;
    }

    /// <summary>
    /// 確認チェックボックスON／OFF切り替え
    /// </summary>
    private void KyknoconfChk_CheckedChanged(object sender, EventArgs e)
    {
        //初期表示中または自動ON／OFFによる再入を無効にする
        if (_isInitializing || _suppressKykCheckEvent)
        {
            return;
        }

        // OFF のときは何もしない
        if (!KyknoconfChk.Checked)
        {
            return;
        }

        if (string.IsNullOrEmpty(HchukyknoTxt.Text) || _currentView == null)
        {
            return;
        }

        try
        {
            Cursor.Current = Cursors.WaitCursor;

            using var db = new DbAccess(_connectionString);

            bool ok = _logic.SetKykInfo(db, _currentView, HchukyknoTxt.Text);

            if (!ok)
            {
                // 契約情報なし表示
                _currentView.IsKykinfoExists = false;
                DispKykInfo(_currentView);

                // チェックを OFF に戻す（イベントの再発生防止）
                try
                {
                    _suppressKykCheckEvent = true;
                    KyknoconfChk.Checked = false;
                }
                finally
                {
                    _suppressKykCheckEvent = false;
                }
                return;
            }

            // 契約情報表示のみ更新
            DispKykInfo(_currentView);

            //・発注時契約Noの入力欄を入力不可にする
            ControlManager.SetEnabledState(HchukyknoTxt, false);
            if (_currentView != null)
            {
                var parent = HchukyknoTxt.Parent;
                var color = parent?.BackColor ?? Color.FromArgb(235, 235, 235);
                HchukyknoTxt.ReadOnlyBackColor =
                    _currentView.Changed.FOCKYKNO
                    ? Color.LightYellow : color;
            }
            HchukyknoTxt.TabStop = false;
        }
        catch (Exception ex)
        {
            SetExceptionLabel();
            Log.Error(ex, "契約情報取得中にエラーが発生しました");
        }
        finally
        {
            Cursor.Current = Cursors.Default;
        }
    }

    /// <summary>
    /// 一時保存ボタン押下時の処理
    /// </summary>
    private void TmpSaveBtn_Click(object sender, EventArgs e)
    {
        try
        {
            //表示初期化
            Cursor.Current = Cursors.WaitCursor;
            SaveMsgLbl.Text = string.Empty;

            //入力チェック
            var validateResult = ValidateInputForSave(SaveAction.Temporary);
            if (!validateResult.Ok)
            {
                SaveMsgLbl.ForeColor = Color.Red;
                SaveMsgLbl.Text = validateResult.Message;
                validateResult.FocusControl?.Focus();
                return;
            }

            //一時保存
            var res = DoSave(SaveAction.Temporary);
            if (res.Ok)
            {
                SetMessageLabel($"一時保存しました。 {DateTime.Now:HH:mm}", MsgType.Info);
                return;
            }

            //排他エラー
            if (!string.IsNullOrEmpty(res.Message))
            {
                SaveMsgLbl.ForeColor = Color.Red;
                SaveMsgLbl.Text = res.Message;
                return;
            }
            //その他エラー
            SetExceptionLabel();

        }
        catch (Exception ex)
        {
            SetExceptionLabel();
            Log.Error(ex, "一時保存ボタン押下時にエラーが発生しました");
        }
        finally
        {
            SaveBtn.Enabled = true;
            TmpSaveBtn.Enabled = true;
            Cursor.Current = Cursors.Default;
        }
    }

    /// <summary>
    /// 確定ボタン押下時の処理
    /// </summary>
    private void SaveBtn_Click(object sender, EventArgs e)
    {
        try
        {
            //表示初期化
            Cursor.Current = Cursors.WaitCursor;
            SaveMsgLbl.Text = string.Empty;

            //入力チェック
            var validateResult = ValidateInputForSave(SaveAction.Confirm);
            if (!validateResult.Ok)
            {
                SaveMsgLbl.ForeColor = Color.Red;
                SaveMsgLbl.Text = validateResult.Message;
                validateResult.FocusControl?.Focus();
                return;
            }

            //確定
            var res = DoSave(SaveAction.Confirm);
            if (res.Ok)
            {
                var confmsg = string.Empty;
                if (GetSelectedNamcd(TenmatsuCmb) == 0)
                {
                    confmsg = $"確定しました。 {DateTime.Now:HH:mm}";
                }
                else
                {
                    confmsg = $"再取得依頼しました。 {DateTime.Now:HH:mm}";
                }
                SetMessageLabel(confmsg, MsgType.Info);
                return;
            }

            //排他エラー
            if (!string.IsNullOrEmpty(res.Message))
            {
                SaveMsgLbl.ForeColor = Color.Red;
                SaveMsgLbl.Text = res.Message;
                return;
            }
            //その他エラー
            SetExceptionLabel();
        }
        catch (Exception ex)
        {
            SetExceptionLabel();
            Log.Error(ex, "確定ボタン押下時にエラーが発生しました");
        }
        finally
        {
            SaveBtn.Enabled = true;
            TmpSaveBtn.Enabled = true;
            Cursor.Current = Cursors.Default;
        }
    }

    #endregion

    #region Privateメソッド
    private void ClearView()
    {
        // ============================
        // 値のクリア
        // ============================
        //■車両情報■
        //CSV作成日
        CsvmkdtTxt.Text = string.Empty;
        //CSV取込日
        CsvimpdtTxt.Text = string.Empty;
        //担当チーム
        TnttmTxt.Text = string.Empty;
        //発注時契約No
        HchukyknoTxt.Text = string.Empty;
        //確認チェックボックス
        KyknoconfChk.Checked = false;

        //契約情報のクリア
        // 契約No
        KyknoTxt.Text = string.Empty;

        // 車名
        CarnmTxt.Text = string.Empty;
        // 型式
        KataTxt.Text = string.Empty;
        // 担当スタッフ
        BucdTxt.Text = string.Empty;
        StfnmTxt.Text = string.Empty;
        // お客様名
        KkyknmTxt.Text = string.Empty;
        // 捺印書類送付先
        DocrcpTxt.Text = string.Empty;
        // 担当チーム
        TnttmTxt.Text = string.Empty;
        // 販売店名
        StrnmTxt.Text = string.Empty;
        //車台番号
        FrmnoTxt.Text = string.Empty;
        //車両特定番号
        FrmsernoTxt.Value = string.Empty;
        //■販売店記入欄■
        //担当者
        StrtntTxt.Text = string.Empty;
        //自動車の種類（自家用／営業用）
        SyuJiRdo.Checked = false;
        SyuEiRdo.Checked = false;
        //自動車の種類（乗用～特種）
        SyuJoRdo.Checked = false;
        Syu4noRdo.Checked = false;
        SyuKeiRdo.Checked = false;
        Syu2toRdo.Checked = false;
        Syu2tuRdo.Checked = false;
        Syu8toRdo.Checked = false;
        SyuSplRdo.Checked = false;
        SyuShrRdo.Checked = false;
        SyukndRdo.Checked = false;
        //用途
        SyuYtoTxt.Text = string.Empty;
        //登録予定日
        TrkytidtTxt.Text = string.Empty;
        //納車予定日
        NsyytidtTxt.Text = string.Empty;
        //納車時間
        TmtNsyTxt.Text = string.Empty;
        //回送先
        TmtpupTxt.Text = string.Empty;
        //都道府県
        Bseuse0Txt.Text = string.Empty;
        //離島
        RitoChk.Checked = false;
        //郵便番号
        Bseuse1Txt.Text = string.Empty;
        //使用本拠の位置
        Bseuse2Txt.Text = string.Empty;
        Bseuse3Txt.Text = string.Empty;

        //書類到着必着日
        DocarvTxt.Text = string.Empty;
        //AM／PM
        Ampm1Rdo.Checked = false;
        Ampm2Rdo.Checked = false;

        //要確認
        DocarvChk.Checked = false;

        //メモ
        Memo1Txt.Text = string.Empty;
        Memo2Txt.Text = string.Empty;
        Memo3Txt.Text = string.Empty;
        Memo4Txt.Text = string.Empty;
    }

    /// <summary>
    /// 画面項目にViewの内容を反映する
    /// </summary>
    /// <param name="view">初期表示用View</param>
    private void DispView(FrameSheetCheckViewModel view)
    {
        // ============================
        // 値のセット
        // ============================
        //■車両情報■
        //CSV作成日
        CsvmkdtTxt.Text = view.FOCOCRDTTM.ToString();
        //CSV取込日
        CsvimpdtTxt.Text = view.FOCIMPDT.ToString();
        ////担当チーム
        //TnttmTxt.Text = view.Kyk_TRKTNTNM ?? string.Empty;
        //発注時契約No
        HchukyknoTxt.Text = view.FOCKYKNO ?? string.Empty;
        //確認チェックボックス
        KyknoconfChk.Checked = view.FOCKYKNOCNF == 1;

        //契約情報の表示
        //担当チーム, 契約No, 車名, 型式, 担当スタッフ, お客様名, 捺印書類送付先, 販売店名
        DispKykInfo(view);

        //車台番号
        FrmnoTxt.Text = view.FOCFRMNO ?? string.Empty;
        //車両特定番号
        FrmsernoTxt.Value = view.FOCFRMSERNO ?? string.Empty;
        //■販売店記入欄■
        //担当者
        StrtntTxt.Text = view.FOCSTRTNT ?? string.Empty;
        //自動車の種類（自家用／営業用）
        SyuJiRdo.Checked = view.FOCSYUJI == "1";
        SyuEiRdo.Checked = view.FOCSYUEI == "1";
        //自動車の種類（乗用～特種）
        SyuJoRdo.Checked = view.FOCSYUJO == "1";
        Syu4noRdo.Checked = view.FOCSYU4NO == "1";
        SyuKeiRdo.Checked = view.FOCSYUKEI == "1";
        Syu2toRdo.Checked = view.FOCSYU2TO == "1";
        Syu2tuRdo.Checked = view.FOCSYU2TU == "1";
        Syu8toRdo.Checked = view.FOCSYU8TO == "1";
        SyuSplRdo.Checked = view.FOCSYUSPL == "1";
        SyuShrRdo.Checked = view.FOCSYUSHR == "1";
        SyukndRdo.Checked = view.FOCSYUKND == "1";
        //用途
        SyuYtoTxt.Text = view.FOCSYUYOT ?? string.Empty;
        //登録予定日
        TrkytidtTxt.Text = view.FOCTRKYTI ?? string.Empty;
        //納車予定日
        NsyytidtTxt.Text = view.FOCNSYYTI ?? string.Empty;
        //納車時間
        TmtNsyTxt.Text = view.FOCTMTNSY ?? string.Empty;
        //回送先
        TmtpupTxt.Text = view.FOCTMTPUP ?? string.Empty;
        //都道府県
        Bseuse0Txt.Text = view.FOCBSEUSE0 ?? string.Empty;
        //離島
        RitoChk.Checked = view.FOCISLAND == 1;
        //郵便番号
        Bseuse1Txt.Text = view.FOCBSEUSE1 ?? string.Empty;
        //使用本拠の位置
        Bseuse2Txt.Text = view.FOCBSEUSE2 ?? string.Empty;
        Bseuse3Txt.Text = view.FOCBSEUSE3 ?? string.Empty;

        //書類到着必着日
        DocarvTxt.Text = view.FOCDOCARV ?? string.Empty;
        //AM／PM
        Ampm1Rdo.Checked = view.FOCAM == "1";
        Ampm2Rdo.Checked = view.FOCPM == "1";

        //要確認
        DocarvChk.Checked = view.FOCDOCARVCNF == 1;

        //メモ
        Memo1Txt.Text = view.FOCMEMO01 ?? string.Empty;
        Memo2Txt.Text = view.FOCMEMO02 ?? string.Empty;
        Memo3Txt.Text = view.FOCMEMO03 ?? string.Empty;
        Memo4Txt.Text = view.FOCMEMO04 ?? string.Empty;

        // ============================
        // 修正済み項目の色変更
        // ============================
        //契約No（発注時）
        var parent = HchukyknoTxt.Parent;
        var color = parent?.BackColor ?? Color.FromArgb(235, 235, 235);
        HchukyknoTxt.ReadOnlyBackColor =
            view.Changed.FOCKYKNO
                ? Color.LightYellow : color;
        //車台番号
        FrmnoTxt.BackColor =
            view.Changed.FOCFRMNO
                ? Color.LightYellow : SystemColors.Window;
        //車両特定番号
        FrmsernoTxt.BackColor =
            view.Changed.FOCFRMSERNO
                ? Color.LightYellow : SystemColors.Window;
        //販売店担当者
        StrtntTxt.BackColor =
            view.Changed.FOCSTRTNT
                ? Color.LightYellow : SystemColors.Window;
        //自動車の種類(自家用／営業用)
        JiEiContainer.BackColor =
            view.Changed.FOCSYUJI
                ? Color.LightYellow : Color.AliceBlue;
        //自動車の種類(種類)
        SyuContainer.BackColor =
            view.Changed.FOCSYUJO
                ? Color.LightYellow : Color.AliceBlue;
        SyuYtoTxt.BackColor =
            view.Changed.FOCSYUJO
                ? Color.LightYellow : SystemColors.Window;
        //登録予定日
        TrkytidtTxt.BackColor =
            view.Changed.FOCTRKYTI
                ? Color.LightYellow : SystemColors.Window;
        //納車予定日
        NsyytidtTxt.BackColor =
            view.Changed.FOCNSYYTI
                ? Color.LightYellow : SystemColors.Window;
        //都道府県
        Bseuse0Txt.BackColor =
            view.Changed.FOCBSEUSE0
                ? Color.LightYellow : SystemColors.Window;
        //郵便番号
        Bseuse1Txt.BackColor =
            view.Changed.FOCBSEUSE1
                ? Color.LightYellow : SystemColors.Window;
        //使用本拠の位置
        Bseuse2Txt.BackColor =
            view.Changed.FOCBSEUSE2
                ? Color.LightYellow : SystemColors.Window;
        Bseuse3Txt.BackColor =
            view.Changed.FOCBSEUSE2
                ? Color.LightYellow : SystemColors.Window;
        //登録書類必着日
        DocarvTxt.BackColor =
            view.Changed.FOCDOCARV
                ? Color.LightYellow : SystemColors.Window;

        SetCombo(TenmatsuCmb, view.ComboItems);

        //書類UT用表示
        if (view.IsDocumentUtMode)
        {
            HchukyknoPnl.BackColor = Color.Tomato;
            KyknoLbl.BackColor = Color.Tomato;
            FrmsernoPnl.BackColor = Color.Tomato;
            DocrcpLbl.BackColor = Color.Tomato;
            DocarvPnl.BackColor = Color.Tomato;
        }
        else
        {
            HchukyknoPnl.BackColor = Color.SteelBlue;
            KyknoLbl.BackColor = Color.SteelBlue;
            FrmsernoPnl.BackColor = Color.SteelBlue;
            DocrcpLbl.BackColor = Color.SteelBlue;
            DocarvPnl.BackColor = Color.SteelBlue;
        }

        TenmatsuCmb.SelectedValue = view.FOCTENMATSU.ToString();

        SaveMsgLbl.Text = string.Empty;

        // 初期スナップショットを保存
        _initialSnapshot = CollectInputToSaveModel();
    }

    /// <summary>
    /// 契約情報の表示
    /// </summary>
    /// <param name="view">viewモデル（契約情報部分のみ使用）</param>
    private void DispKykInfo(FrameSheetCheckViewModel view)
    {
        // 契約No
        KyknoTxt.Text = view.Kyk_LSKYKNO ?? string.Empty;

        // 車名
        if (!view.IsKykinfoExists)
        {
            CarnmTxt.Text = "契約情報なし";
            CarnmTxt.ActiveForeColor = Color.Red;
            CarnmTxt.ReadOnlyForeColor = Color.Red;
        }
        else
        {
            CarnmTxt.Text = view.Kyk_SHDNCARNM ?? string.Empty;
            CarnmTxt.ActiveForeColor = SystemColors.WindowText;
            CarnmTxt.ReadOnlyForeColor = SystemColors.WindowText;
        }

        // 型式
        KataTxt.Text = view.Kyk_KATA ?? string.Empty;

        // 担当スタッフ
        BucdTxt.Text = view.Kyk_EGYTNTBUCD ?? string.Empty;
        StfnmTxt.Text = view.Kyk_EGYTNTNM ?? string.Empty;

        // お客様名
        KkyknmTxt.Text = view.Kyk_KYKSKNMJDN ?? string.Empty;

        // 捺印書類送付先
        DocrcpTxt.Text = view.Kyk_NTINCHK_DS ?? string.Empty;

        // 担当チーム
        TnttmTxt.Text = view.Kyk_TRKTNTNM ?? string.Empty;

        // 販売店名
        StrnmTxt.Text = view.Kyk_HCHSKNMJDN ?? string.Empty;
    }

    /// <summary>
    /// 入力チェック処理
    /// </summary>
    /// <param name="action">一時保存or確定</param>
    private SaveResult ValidateInputForSave(SaveAction action)
    {
        var result = new SaveResult();

        // 一時保存または再取得依頼のフラグ
        bool isNeedChk = false;
        if (action == SaveAction.Confirm && GetSelectedNamcd(TenmatsuCmb) == 0)
        {
            isNeedChk = true;
        }

        var errors = new List<UiError>();

        // 全てのエラークリア（アイコンだけ）
        ClearAllErrorLabels();

        // 発注時契約Noのチェック
        //未入力
        if (isNeedChk && string.IsNullOrWhiteSpace(HchukyknoTxt.Text))
        {
            errors.Add(new UiError(HchukyknoTxt, HchukyknoLbl, "未入力です"));
        }
        //桁数
        if (errors.Count == 0)
        {
            ChkMaxLengthError(errors, HchukyknoTxt, HchukyknoLbl, 7, "文字超過(７桁まで)");
        }
        // 存在チェック
        if (isNeedChk && errors.Count == 0)
        {
            using var db = new DbAccess(_connectionString);
            var chkExists = _logic.GetTLTL2001(db, HchukyknoTxt.Text);
            if (chkExists == 0)
            {
                errors.Add(new UiError(HchukyknoTxt, HchukyknoLbl, "登録のない契約Noです"));
            }
        }
        //確認CheckBox
        if (isNeedChk && errors.Count == 0 && !KyknoconfChk.Checked)
        {
            errors.Add(new UiError(HchukyknoTxt, HchukyknoLbl, "確認チェックなし"));
        }

        //車台番号
        ChkMaxLengthError(errors, FrmnoTxt, FrmnoLbl, 60, "文字超過(半角60文字まで)");

        //車両特定番号
        ChkMaxLengthError(errors, FrmsernoTxt, FrmsernoLbl, 100, "文字超過(半角100文字まで)");

        //担当者
        ChkMaxLengthError(errors, StrtntTxt, StrtntLbl, 100, "文字超過(半角100文字分まで)");

        if (isNeedChk)
        {
            //自動車の種類(自家用 / 営業用)
            bool isSyuJiEiSelected = SyuJiRdo.Checked || SyuEiRdo.Checked;
            if (!isSyuJiEiSelected)
            {
                errors.Add(new UiError(SyuJiRdo, SyunmLbl, "未選択です"));
            }

            //自動車の種類(下段)（乗用～特種）
            bool isSyuTypeSelected = SyuJoRdo.Checked || Syu4noRdo.Checked || SyuKeiRdo.Checked ||
                                    Syu2toRdo.Checked || Syu2tuRdo.Checked ||
                                    Syu8toRdo.Checked || SyuSplRdo.Checked ||
                                    SyuShrRdo.Checked || SyukndRdo.Checked;
            if (!isSyuTypeSelected)
            {
                errors.Add(new UiError(SyuJoRdo, SyunmLbl, "未選択です"));
            }
        }

        //自動車の種類(用途)
        ChkMaxLengthError(errors, SyuYtoTxt, SyuYtoLbl, 200, "文字超過(半角200文字分まで)");

        //登録予定日
        ChkMaxLengthError(errors, TrkytidtTxt, TrkytidtLbl, 20, "文字超過(半角20文字分まで)");

        //納車予定日
        ChkMaxLengthError(errors, NsyytidtTxt, NsyytidtLbl, 20, "文字超過(半角20文字分まで)");

        //納車時間
        ChkMaxLengthError(errors, TmtNsyTxt, TmtNsyLbl, 20, "文字超過(半角20文字分まで)");

        //回送先
        ChkMaxLengthError(errors, TmtpupTxt, TmtpupLbl, 20, "文字超過(半角20文字分まで)");

        //都道府県
        ChkMaxLengthError(errors, Bseuse0Txt, Bseuse0Lbl, 20, "文字超過(半角20文字分まで)");

        //郵便番号
        ChkMaxLengthError(errors, Bseuse1Txt, Bseuse1Lbl, 100, "文字超過(半角100文字分まで)");

        //使用本拠の位置（上段）
        ChkMaxLengthError(errors, Bseuse2Txt, Bseuse2Lbl, 200, "文字超過(半角200文字分まで)");

        //使用本拠の位置（下段）
        ChkMaxLengthError(errors, Bseuse3Txt, Bseuse2Lbl, 200, "文字超過(半角200文字分まで)");

        //書類到着必着日
        ChkMaxLengthError(errors, DocarvTxt, DocarvLbl, 20, "文字超過(半角20文字分まで)");

        //メモ1
        ChkMaxLengthError(errors, Memo1Txt, MemoLbl, 50, "文字超過(半角50文字分まで)");

        //メモ2
        ChkMaxLengthError(errors, Memo2Txt, MemoLbl, 50, "文字超過(半角50文字分まで)");

        //メモ3
        ChkMaxLengthError(errors, Memo3Txt, MemoLbl, 50, "文字超過(半角50文字分まで)");

        //メモ4
        ChkMaxLengthError(errors, Memo4Txt, MemoLbl, 50, "文字超過(半角50文字分まで)");

        //まとめてエラーアイコン表示
        DispErrors(errors);
        if (errors.Count > 0)
        {
            result.Ok = false;
            result.Message = "入力内容に不備があります。修正してから再度実行してください。";
            result.FocusControl = errors[0].Input;
            return result;
        }

        //チェックOK
        result.Ok = true;
        return result;
    }

    /// <summary>
    /// 最大桁数のエラーチェック（GcTextBox用）
    /// </summary>
    private void ChkMaxLengthError(List<UiError> errors, GcTextBox textBox, Label label, int maxLength, string message)
    {
        if (!string.IsNullOrEmpty(textBox.Text))
        {
            int halfWidthLength = Encoding.GetEncoding("shift_jis").GetByteCount(textBox.Text);

            if (halfWidthLength > maxLength)
            {
                errors.Add(new UiError(textBox, label, message));
            }
        }
    }

    /// <summary>
    /// 最大桁数のエラーチェック（GcMask用）
    /// </summary>
    private void ChkMaxLengthError(List<UiError> errors, GcMask textBox, Label label, int maxLength, string message)
    {
        if (!string.IsNullOrEmpty(textBox.Value))
        {
            int halfWidthLength = Encoding.GetEncoding("shift_jis").GetByteCount(textBox.Value);

            if (halfWidthLength > maxLength)
            {
                errors.Add(new UiError(textBox, label, message));
            }
        }
    }

    /// <summary>
    /// 保存処理共通
    /// </summary>
    /// <param name="action">一時保存or確定</param>
    private SaveResult DoSave(SaveAction action)
    {

        var result = new SaveResult();

        if (_currentView == null)
        {
            result.Ok = false;
            result.Message = "画面データが読み込まれていません。";
            return result;
        }

        try
        {
            //画面の入力値を取得
            var save = CollectInputToSaveModel();

            //ステータス
            if (action == SaveAction.Confirm)
            {
                if (save.FOCTENMATSU == 1)
                {
                    //顛末コンボボックスが「1:再取得依頼」のときは、
                    //ステータスを「2:再送付依頼」で更新する
                    save.FOCSTS = 2;
                }

                //確定日時
                save.FOCCNFDATE = DateTime.Now;
                //確定者
                save.FOCCNFUSR = _userId;
            }
            else
            {
                //一時保存
                save.FOCSTS = 1;
            }

            //楽観ロック用（表示時点の更新日時）
            save.OldUpdDate = _currentView.FOCUPDDATE;

            //今回更新の値（新値）
            save.FOCUPDDATE = DateTime.Now;
            save.FOCUPDUSR = _userId;

            //保存
            _logic.Save(save);

            //保存後に再ロード
            var reloaded = _logic.LoadInitial(FCOID);
            _currentView = reloaded;
            if (reloaded != null)
            {
                DispView(reloaded);
            }

            result.Ok = true;
            result.Message = string.Empty;
            return result;
        }
        catch (InvalidOperationException ex)
        {
            //排他エラーを拾う
            result.Ok = false;
            result.Message = ex.Message;
            Log.Error(ex, "保存中にエラーが発生しました。");
            return result;
        }
        catch (Exception ex)
        {
            result.Ok = false;
            Log.Error(ex, "保存中にエラーが発生しました。");
            return result;
        }
    }

    /// <summary>
    /// 画面値をSaveModelに格納する
    /// </summary>
    /// <returns>FrameSheetCheckSaveModel</returns>
    private FrameSheetCheckSaveModel CollectInputToSaveModel()
    {
        return new FrameSheetCheckSaveModel
        {
            FOCFCOID = FCOID,
            FOCBUNM = TnttmTxt.Text,

            // --------------------
            // 車両情報
            // --------------------
            FOCKYKNO = HchukyknoTxt.Text,
            FOCFRMNO = FrmnoTxt.Text,
            FOCFRMSERNO = FrmsernoTxt.Value,

            // --------------------
            // 販売店記入欄
            // --------------------
            FOCSTRTNT = StrtntTxt.Text,
            // 0/1（文字）に統一
            FOCSYUJI = SyuJiRdo.Checked ? "1" : "0",
            FOCSYUEI = SyuEiRdo.Checked ? "1" : "0",
            FOCSYUJO = SyuJoRdo.Checked ? "1" : "0",
            FOCSYU4NO = Syu4noRdo.Checked ? "1" : "0",
            FOCSYUKEI = SyuKeiRdo.Checked ? "1" : "0",
            FOCSYU2TO = Syu2toRdo.Checked ? "1" : "0",
            FOCSYU2TU = Syu2tuRdo.Checked ? "1" : "0",
            FOCSYU8TO = Syu8toRdo.Checked ? "1" : "0",
            FOCSYUSPL = SyuSplRdo.Checked ? "1" : "0",
            FOCSYUSHR = SyuShrRdo.Checked ? "1" : "0",
            FOCSYUKND = SyukndRdo.Checked ? "1" : "0",

            FOCSYUYOT = SyuYtoTxt.Text,
            FOCTRKYTI = TrkytidtTxt.Text,
            FOCNSYYTI = NsyytidtTxt.Text,
            FOCTMTNSY = TmtNsyTxt.Text,
            FOCTMTPUP = TmtpupTxt.Text,
            FOCBSEUSE0 = Bseuse0Txt.Text,
            FOCBSEUSE1 = Bseuse1Txt.Text,
            FOCBSEUSE2 = Bseuse2Txt.Text,
            FOCBSEUSE3 = Bseuse3Txt.Text,
            FOCDOCARV = DocarvTxt.Text,

            FOCAM = Ampm1Rdo.Checked ? "1" : "0",
            FOCPM = Ampm2Rdo.Checked ? "1" : "0",

            // byteフラグ
            FOCDOCARVCNF = (byte)(DocarvChk.Checked ? 1 : 0),
            FOCISLAND = (byte)(RitoChk.Checked ? 1 : 0),
            FOCKYKNOCNF = (byte)(KyknoconfChk.Checked ? 1 : 0),

            FOCMEMO01 = Memo1Txt.Text,
            FOCMEMO02 = Memo2Txt.Text,
            FOCMEMO03 = Memo3Txt.Text,
            FOCMEMO04 = Memo4Txt.Text,

            FOCTENMATSU = GetSelectedNamcd(TenmatsuCmb),
        };
    }

    /// <summary>
    /// 顛末コンボボックスの値をセット
    /// </summary>
    private void SetCombo(GcComboBox combo, IReadOnlyList<Common.Db.JGSVNAMReader.NameItem> items)
    {
        combo.ListColumns.Clear();
        combo.Items.Clear();

        //表示に使うサブアイテムは 0 番（名称）
        combo.TextSubItemIndex = 0;

        //コードをValueとして扱う
        combo.ValueSubItemIndex = 1;

        //Items を作る（SubItem[0]=名称、SubItem[1]=コード）
        var listItems = new List<ListItem>();

        // ===== 先頭の空白行 =====
        var emptyItem = new ListItem();
        emptyItem.SubItems.AddRange(new[]
        {
            new SubItem { Value = string.Empty },   //表示
            new SubItem { Value = "0" },            //内部コード
        });
        listItems.Add(emptyItem);

        // ===== 通常データ =====
        foreach (var item in items)
        {
            var name = item.NAMVAL1 ?? string.Empty;
            var code = item.NAMCD ?? "0";

            var li = new ListItem();
            li.SubItems.AddRange(new[]
            {
                new SubItem { Value = name },   //表示
                new SubItem { Value = code },   //内部コード
            });

            listItems.Add(li);
        }

        combo.Items.AddRange(listItems.ToArray());

        // 初期選択を空白行にする
        combo.SelectedIndex = 0;
    }

    /// <summary>
    /// コンボボックスの選択値取得
    /// </summary>
    private byte GetSelectedNamcd(GcComboBox combo)
    {
        var li = combo.SelectedItem as ListItem;
        var selectval = li.SubItems[1].Value ?? 0;
        var val = ConvertValue.ToByteOr0(selectval);

        return val;
    }

    /// <summary>
    /// エラーアイコンの表示
    /// </summary>
    /// <param name="errors">エラーのあるコントロールのリスト</param>
    private void DispErrors(List<UiError> errors)
    {

        // エラーをセット
        for (int i = 0; i < errors.Count; i++)
        {
            UiError e = errors[i];

            _errorProvider.SetIconAlignment(e.Label, ErrorIconAlignment.MiddleLeft);
            _errorProvider.SetError(e.Label, e.Message);
        }
    }

    /// <summary>
    /// エラーアイコンのクリア
    /// </summary>
    private void ClearAllErrorLabels()
    {
        _errorProvider.SetError(HchukyknoLbl, string.Empty);
        _errorProvider.SetError(FrmnoLbl, string.Empty);
        _errorProvider.SetError(FrmsernoLbl, string.Empty);
        _errorProvider.SetError(StrtntLbl, string.Empty);
        _errorProvider.SetError(SyunmLbl, string.Empty);
        _errorProvider.SetError(SyuYtoLbl, string.Empty);
        _errorProvider.SetError(TrkytidtLbl, string.Empty);
        _errorProvider.SetError(TmtNsyLbl, string.Empty);
        _errorProvider.SetError(TmtpupLbl, string.Empty);
        _errorProvider.SetError(Bseuse0Lbl, string.Empty);
        _errorProvider.SetError(Bseuse1Lbl, string.Empty);
        _errorProvider.SetError(Bseuse2Lbl, string.Empty);
        _errorProvider.SetError(DocarvLbl, string.Empty);
        _errorProvider.SetError(MemoLbl, string.Empty);
    }

    /// <summary>
    /// 画面下部にメッセージを表示する
    /// </summary>
    /// <param name="message">メッセージ内容</param>
    /// <param name="type">Error：エラー、Info：通常</param>
    private void SetMessageLabel(string message, MsgType type)
    {
        if (type == MsgType.Error)
        {
            SaveMsgLbl.ForeColor = Color.Red;
        }
        else
        {
            SaveMsgLbl.ForeColor = Color.Blue;
        }
        SaveMsgLbl.Text = message;
    }

    /// <summary>
    /// 例外エラー発生時
    /// </summary>
    private void SetExceptionLabel()
    {
        SaveMsgLbl.ForeColor = Color.Red;
        SaveMsgLbl.Text = "予期せぬエラーが発生しました。管理者にお問い合わせください。";
    }

    /// <summary>
    /// 初期値と現在値の比較
    /// </summary>
    /// <returns>True:変更あり, False:変更なし</returns>
    private bool IsModified()
    {
        if (_initialSnapshot == null)
        {
            return false;
        }

        var current = CollectInputToSaveModel();

        var diff = FindFirstDifference<FrameSheetCheckSaveModel>(current, _initialSnapshot);
        if (diff != null)
        {
            return true;
        }

        return false;
    }

    private (string Name, object? Current, object? Initial)? FindFirstDifference<T>(T current, T initial)
    {
        var props = typeof(T).GetProperties()
            .Where(p => p.CanRead);

        foreach (var p in props)
        {
            var cur = p.GetValue(current);
            var ini = p.GetValue(initial);

            // 正規化（重要）
            if (cur is string cs && string.IsNullOrWhiteSpace(cs))
            {
                cur = null;
            }
            if (ini is string isv && string.IsNullOrWhiteSpace(isv))
            {
                ini = null;
            }

            if (!Equals(cur, ini))
            {
                return (p.Name, cur, ini);
            }
        }

        return null;
    }

    #endregion

    #region Privateメソッド（PdfViewer関連）

    /// <summary>
    /// PDFViewerのプロパティセット
    /// </summary>
    private void SetPdfViewerProperty(string fileNm)
    {
        try
        {
            //プロパティ設定
            PdfViewer1.Dock = DockStyle.Fill;
            PdfViewer1.Document = PdfDocument.Load(fileNm);
            PdfViewer1.ZoomMode = PdfiumViewer.PdfViewerZoomMode.FitWidth;

            //スクロールイベント
            PdfViewer1.Renderer.Scroll += Renderer_Scroll;
            PdfViewer1.Renderer.MouseWheel += Renderer_MouseWheel;

            //印刷ボタン非表示
            var toolStrip2 = PdfViewer1.Controls.OfType<ToolStrip>().FirstOrDefault();
            if (toolStrip2 != null)
            {
                foreach (ToolStripItem item in toolStrip2.Items)
                {
                    if (item is ToolStripButton && item.Text == "Print")
                    {
                        toolStrip2.Items.Remove(item);
                        break;
                    }
                }

            }

            toolStrip2?.Items.Add(new ToolStripSeparator());

            ToolStripButton toolbtnLeft = new ToolStripButton
            {
                Text = "左回転",
                Name = "btnLeft",
            };
            // クリックイベントを追加
            toolbtnLeft.Click += BtnLeft_Click;
            // ToolStripに追加
            toolStrip2?.Items.Add(toolbtnLeft);

            ToolStripButton toolbtnRight = new ToolStripButton
            {
                Text = "右回転",
                Name = "btnRight",
            };
            // クリックイベントを追加
            toolbtnRight.Click += BtnRight_Click;
            // ToolStripに追加
            toolStrip2?.Items.Add(toolbtnRight);

            ToolStripButton toolbtnUpsideDown = new ToolStripButton
            {
                Text = "半回転",
                Name = "btnUpsideDown",
            };
            // クリックイベントを追加
            toolbtnUpsideDown.Click += BtnUpsideDown_Click;
            // ToolStripに追加
            toolStrip2?.Items.Add(toolbtnUpsideDown);

            ToolStripButton toolbtnPrev = new ToolStripButton
            {
                Text = "＜",
                Name = "btnPrev",
            };

            // クリックイベントを追加
            toolbtnPrev.Click += BtnPrev_Click;
            // ToolStripに追加
            toolStrip2?.Items.Add(toolbtnPrev);
            toolStrip2?.Items.Add(new ToolStripLabel("0", null, false, null, name: "lbl1"));
            toolStrip2?.Items.Add(new ToolStripLabel("/", null, false, null, name: "lbl2"));
            toolStrip2?.Items.Add(new ToolStripLabel("0", null, false, null, name: "lbl3"));

            ToolStripButton toolbtnNext = new ToolStripButton
            {
                Text = "＞",
                Name = "btnNext",
            };
            // クリックイベントを追加
            toolbtnPrev.Click += BtnNext_Click;
            // ToolStripに追加
            toolStrip2?.Items.Add(toolbtnNext);

            //ページ表示
            ShowPageCount();
        }
        catch (Exception ex)
        {

            SetExceptionLabel();
            Log.Error(ex, "PDFViewerのプロパティセット中にエラーが発生しました。");

        }
    }

    // マウスホイールでのPDFスクロール
    private void Renderer_MouseWheel(object? sender, MouseEventArgs e)
    {
        ShowPageCount();
    }

    // マウスでスクロールバーを操作するスクロール
    private void Renderer_Scroll(object? sender, ScrollEventArgs e)
    {
        ShowPageCount();
    }

    // 前へボタン
    private void BtnPrev_Click(object? sender, EventArgs e)
    {

        if (PdfViewer1.Renderer.Page < 0)
        {
            return;
        }
        int currentPage = PdfViewer1.Renderer.Page - 1;

        PdfViewer1.Renderer.Page = currentPage;

        // ToolStrip を取得（なければ作成して追加）
        var toolStrip2 = PdfViewer1.Controls.OfType<ToolStrip>().FirstOrDefault();
        if (toolStrip2 is null)
        {
            toolStrip2 = new ToolStrip { Name = "toolStrip2" };
            PdfViewer1.Controls.Add(toolStrip2);
        }

        // "lbl1" を ToolStripLabel として取得（なければ作成して追加）
        var lbl1 = toolStrip2.Items["lbl1"] as ToolStripLabel;
        if (lbl1 is null)
        {
            lbl1 = new ToolStripLabel { Name = "lbl1" };
            toolStrip2.Items.Add(lbl1);
        }

        // Renderer の存在を確認してからページ番号を設定
        if (PdfViewer1.Renderer is not null)
        {
            lbl1.Text = (PdfViewer1.Renderer.Page + 1).ToString();
        }
        else
        {
            // 必要に応じてフォールバック
            lbl1.Text = "-";
        }
    }

    // 次へボタン
    private void BtnNext_Click(object? sender, EventArgs e)
    {
        if (PdfViewer1.Renderer.Page >= PdfViewer1.Document.PageCount)
        {
            return;
        }

        int currentPage = PdfViewer1.Renderer.Page + 1;

        PdfViewer1.Renderer.Page = currentPage;
        // ToolStrip を取得（なければ作成して追加）
        var toolStrip2 = PdfViewer1.Controls.OfType<ToolStrip>().FirstOrDefault();
        if (toolStrip2 is null)
        {
            toolStrip2 = new ToolStrip { Name = "toolStrip2" };
            PdfViewer1.Controls.Add(toolStrip2);
        }

        // "lbl3" を ToolStripLabel として取得（なければ作成して追加）
        var lbl3 = toolStrip2.Items["lbl3"] as ToolStripLabel;
        if (lbl3 is null)
        {
            lbl3 = new ToolStripLabel { Name = "lbl3" };
            toolStrip2.Items.Add(lbl3);
        }

        // Renderer の存在を確認してからページ番号を設定
        if (PdfViewer1.Renderer is not null)
        {
            lbl3.Text = (PdfViewer1.Renderer.Page + 1).ToString();
        }
        else
        {
            // 必要に応じてフォールバック
            lbl3.Text = "-";
        }
    }

    // 右回転
    private void BtnRight_Click(object? sender, EventArgs e)
    {
        PdfViewer1.Renderer.RotateRight();
    }

    // 左回転
    private void BtnLeft_Click(object? sender, EventArgs e)
    {
        PdfViewer1.Renderer.RotateLeft();
    }

    // 半回転
    private void BtnUpsideDown_Click(object? sender, EventArgs e)
    {
        PdfViewer1.Renderer.RotateLeft();
        PdfViewer1.Renderer.RotateLeft();
    }

    /// <summary>
    /// ページ数の表示
    /// </summary>
    private void ShowPageCount()
    {
        if (PdfViewer1.Document == null || PdfViewer1.Renderer == null)
        {
            return;
        }

        int currentPage = PdfViewer1.Renderer.Page + 1;
        int pageCount = PdfViewer1.Document.PageCount;
        // ToolStrip を取得（なければ作成して追加）
        var toolStrip2 = PdfViewer1.Controls.OfType<ToolStrip>().FirstOrDefault();
        if (toolStrip2 is null)
        {
            toolStrip2 = new ToolStrip { Name = "toolStrip2" };
            PdfViewer1.Controls.Add(toolStrip2);
        }

        // "lbl1" を ToolStripLabel として取得（なければ作成して追加）
        var lbl1 = toolStrip2.Items["lbl1"] as ToolStripLabel;
        if (lbl1 is null)
        {
            lbl1 = new ToolStripLabel { Name = "lbl1" };
            toolStrip2.Items.Add(lbl1);
        }

        // "lbl3" を ToolStripLabel として取得（なければ作成して追加）
        var lbl3 = toolStrip2.Items["lbl3"] as ToolStripLabel;
        if (lbl3 is null)
        {
            lbl3 = new ToolStripLabel { Name = "lbl3" };
            toolStrip2.Items.Add(lbl3);
        }

        // Renderer の存在を確認してからページ番号を設定
        if (PdfViewer1.Renderer is not null)
        {
            lbl1.Text = currentPage.ToString();
            lbl3.Text = pageCount.ToString();
        }
        else
        {
            // 必要に応じてフォールバック
            lbl1.Text = "-";
        }

    }

    #endregion

    #region 入力エラー情報格納用クラス

    private class UiError
    {
        public UiError(Control input, Control label, string message)
        {
            Input = input;      //フォーカス先
            Label = label;      //エラーアイコン表示先
            Message = message;  //表示メッセージ
        }

        public Control Input { get; }

        public Control Label { get; }

        public string Message { get; }

    }

    #endregion

    #region 保存結果格納用クラス

    private class SaveResult
    {
        public bool Ok { get; set; }

        public string Message { get; set; } = string.Empty;

        public Control? FocusControl { get; set; }
    }

    #endregion

}