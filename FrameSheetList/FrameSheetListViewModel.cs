namespace DesktopApp.FrameSheetList;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

/// <summary>
/// 車台番号連絡票一覧画面の表示用クラス
/// </summary>
internal sealed class FrameSheetListViewModel
{
    /// <summary>一覧表示用の行データ</summary>
    public IReadOnlyList<FrameSheetListRowViewModel> Items { get; set; } = Array.Empty<FrameSheetListRowViewModel>();

    /// <summary>選択行</summary>
    public FrameSheetListRowViewModel? SelectedItem { get; set; }

    /// <summary>検索条件</summary>
    public FrameSheetListSearchConditions Conditions { get; set; } = new FrameSheetListSearchConditions();

    /// <summary>表示用サマリ</summary>
    public FrameSheetListSummary Summary { get; set; } = new FrameSheetListSummary();
}

/// <summary>
/// 検索条件
/// </summary>
internal sealed class FrameSheetListSearchConditions : INotifyPropertyChanged
{
    private bool _condTntutRegister;
    private bool _condTntutDocument;

    private bool _condCsvtypNormal;
    private bool _condCsvtypTmt;

    private bool _condStsUnregistered;
    private bool _condStsTemporary;
    private bool _condStsResendRequest;
    private bool _condStsConfirmed;

    private DateTime? _condImpdtFrom;
    private DateTime? _condImpdtTo;

    private string? _condHchkykno;
    private string? _condFrmno;
    private string? _condFrmserno;
    private string? _condStrnm;
    private string? _condKkyknm;

    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>担当UT（登録UT）</summary>
    public bool CondTNTUT_Register
    {
        get => this._condTntutRegister;
        set => this.SetProperty(ref this._condTntutRegister, value);
    }

    /// <summary>担当UT（書類UT）</summary>
    public bool CondTNTUT_Document
    {
        get => this._condTntutDocument;
        set => this.SetProperty(ref this._condTntutDocument, value);
    }

    /// <summary>種別（通常）</summary>
    public bool CondCSVTYP_Normal
    {
        get => this._condCsvtypNormal;
        set => this.SetProperty(ref this._condCsvtypNormal, value);
    }

    /// <summary>種別（TMT）</summary>
    public bool CondCSVTYP_Tmt
    {
        get => this._condCsvtypTmt;
        set => this.SetProperty(ref this._condCsvtypTmt, value);
    }

    /// <summary>ステータス（未登録）</summary>
    public bool CondSTS_Unregistered
    {
        get => this._condStsUnregistered;
        set => this.SetProperty(ref this._condStsUnregistered, value);
    }

    /// <summary>ステータス（一時保存）</summary>
    public bool CondSTS_Temporary
    {
        get => this._condStsTemporary;
        set => this.SetProperty(ref this._condStsTemporary, value);
    }

    /// <summary>ステータス（再送付依頼）</summary>
    public bool CondSTS_ResendRequest
    {
        get => this._condStsResendRequest;
        set => this.SetProperty(ref this._condStsResendRequest, value);
    }

    /// <summary>ステータス（確定済）</summary>
    public bool CondSTS_Confirmed
    {
        get => this._condStsConfirmed;
        set => this.SetProperty(ref this._condStsConfirmed, value);
    }

    /// <summary>取込日 From</summary>
    public DateTime? CondIMPDTFrom
    {
        get => this._condImpdtFrom;
        set => this.SetProperty(ref this._condImpdtFrom, value);
    }

    /// <summary>取込日 To</summary>
    public DateTime? CondIMPDTTo
    {
        get => this._condImpdtTo;
        set => this.SetProperty(ref this._condImpdtTo, value);
    }

    /// <summary>発注時契約No</summary>
    public string? CondHCHKYKNO
    {
        get => this._condHchkykno;
        set => this.SetProperty(ref this._condHchkykno, value);
    }

    /// <summary>車台番号</summary>
    public string? CondFRMNO
    {
        get => this._condFrmno;
        set => this.SetProperty(ref this._condFrmno, value);
    }

    /// <summary>車両特定番号</summary>
    public string? CondFRMSERNO
    {
        get => this._condFrmserno;
        set => this.SetProperty(ref this._condFrmserno, value);
    }

    /// <summary>販売店名</summary>
    public string? CondSTRNM
    {
        get => this._condStrnm;
        set => this.SetProperty(ref this._condStrnm, value);
    }

    /// <summary>お客様名</summary>
    public string? CondKKYKNM
    {
        get => this._condKkyknm;
        set => this.SetProperty(ref this._condKkyknm, value);
    }

    /// <summary>
    /// 条件を初期化する（クリアボタン用）
    /// </summary>
    public void Clear()
    {
        this.CondTNTUT_Register = false;
        this.CondTNTUT_Document = false;

        this.CondCSVTYP_Normal = false;
        this.CondCSVTYP_Tmt = false;

        this.CondSTS_Unregistered = false;
        this.CondSTS_Temporary = false;
        this.CondSTS_ResendRequest = false;
        this.CondSTS_Confirmed = false;

        this.CondIMPDTFrom = null;
        this.CondIMPDTTo = null;

        this.CondHCHKYKNO = null;
        this.CondFRMNO = null;
        this.CondFRMSERNO = null;
        this.CondSTRNM = null;
        this.CondKKYKNM = null;
    }

    /// <summary>
    /// 初回表示時の検索条件へ初期化する
    /// </summary>
    public void InitializeForFirstDisplay()
    {
        this.Clear();
        this.CondSTS_Unregistered = true;
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        this.OnPropertyChanged(name);
        return true;
    }
}

/// <summary>
/// 表示制御（件数などの集計値）
/// </summary>
internal sealed class FrameSheetListSummary
{
    /// <summary>件数 全件</summary>
    public int CNT_TOTAL { get; set; }

    /// <summary>件数 確認前（STATUS=9 or 99）</summary>
    public int CNT_KAKUNINMAE { get; set; }

    /// <summary>件数 一時保存（STATUS=1）</summary>
    public int CNT_ICHIZON { get; set; }
}

/// <summary>
/// 一覧の1行分（SQLのUNION結果1レコードに対応）
/// </summary>
internal sealed class FrameSheetListRowViewModel
{
    /// <summary>ID（OCR:FCOID / TMT:FCTID）</summary>
    public int ID { get; set; }

    /// <summary>取込種別（1:通常 / 2:TMT）</summary>
    public int CSVTYP { get; set; }

    /// <summary>取込種別名（通常 / TMT）</summary>
    public string? CSVTYPNM { get; set; }

    /// <summary>ファイル作成日（OCRDTTM）</summary>
    public DateTime? CRTDTTM { get; set; }

    /// <summary>取込日時</summary>
    public DateTime? IMPDT { get; set; }

    /// <summary>登録書類必着日</summary>
    public DateTime? DOCARV { get; set; }

    /// <summary>発注時契約No</summary>
    public string? HCHKYKNO { get; set; }

    /// <summary>車台番号</summary>
    public string? FRMNO { get; set; }

    /// <summary>車両特定番号</summary>
    public string? FRMSERNO { get; set; }

    /// <summary>販売店名1</summary>
    public string? STRNM1 { get; set; }

    /// <summary>販売店名2</summary>
    public string? STRNM2 { get; set; }

    /// <summary>販売店名1,2を結合したもの</summary>
    public string? STRNM { get; set; }

    /// <summary>お客様名</summary>
    public string? KKYKNM { get; set; }

    /// <summary>ステータスコード（SQL側で99補完済み）</summary>
    public int STATUS { get; set; }

    /// <summary>ステータス名（SQL側で'未処理'補完済み）</summary>
    public string? STATUSNM { get; set; }

    /// <summary>担当UT（1:登録UT / 2:書類UT）</summary>
    public int TNTUT { get; set; }

    /// <summary>担当UT名</summary>
    public string? TNTUTNM { get; set; }
}
