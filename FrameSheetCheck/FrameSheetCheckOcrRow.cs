namespace DesktopApp.FrameSheetCheck;

/// <summary>
/// 車台番号連絡票CSV取込テーブル（OCR）（JGSTFRMCSVOCR）テーブルからデータを取得するためのクラス
/// </summary>
internal sealed class FrameSheetOcrRow
{
    /// <summary>取込ID</summary>
    public int FCOID { get; set; }

    /// <summary>取込ファイル名</summary>
    public string FCOIMPFILENM { get; set; } = string.Empty;

    /// <summary>レコード番号（ヘッダー除く）</summary>
    public int FCOIMPRCDCNT { get; set; }

    /// <summary>ファイル名</summary>
    public string FCOFILENM { get; set; } = string.Empty;

    /// <summary>帳票名</summary>
    public string FCOFORMNM { get; set; } = string.Empty;

    /// <summary>販売店名1</summary>
    public string FCOSTRNM1 { get; set; } = string.Empty;

    /// <summary>販売店名2</summary>
    public string FCOSTRNM2 { get; set; } = string.Empty;

    /// <summary>販売店営業所名</summary>
    public string FCOSTROFSNM { get; set; } = string.Empty;

    /// <summary>販売店郵便番号</summary>
    public string FCOSTRPSTNO { get; set; } = string.Empty;

    /// <summary>販売店住所1</summary>
    public string FCOSTRADD1 { get; set; } = string.Empty;

    /// <summary>販売店住所2</summary>
    public string FCOSTRADD2 { get; set; } = string.Empty;

    /// <summary>販売店電話番号</summary>
    public string FCOSTRTEL { get; set; } = string.Empty;

    /// <summary>契約番号</summary>
    public string FCOKYKNO { get; set; } = string.Empty;

    /// <summary>車名</summary>
    public string FCOCARNM { get; set; } = string.Empty;

    /// <summary>型式</summary>
    public string FCOKATA { get; set; } = string.Empty;

    /// <summary>TMS部署名</summary>
    public string FCOBUNM { get; set; } = string.Empty;

    /// <summary>TMS担当スタッフ名</summary>
    public string FCOTNTSTFNM { get; set; } = string.Empty;

    /// <summary>お客様名1</summary>
    public string FCOKKYKNM1 { get; set; } = string.Empty;

    /// <summary>お客様名2</summary>
    public string FCOKKYKNM2 { get; set; } = string.Empty;

    /// <summary>OSS申請有無</summary>
    public string FCOOSS { get; set; } = string.Empty;

    /// <summary>販売店担当者名</summary>
    public string FCOSTRTNT { get; set; } = string.Empty;

    /// <summary>販売店メールアドレス</summary>
    public string FCOSTREML { get; set; } = string.Empty;

    /// <summary>記入日</summary>
    public string FCOENTDAY { get; set; } = string.Empty;

    /// <summary>自家用区分</summary>
    public string FCOSYUJI { get; set; } = string.Empty;

    /// <summary>営業用区分</summary>
    public string FCOSYUEI { get; set; } = string.Empty;

    /// <summary>乗用区分</summary>
    public string FCOSYUJO { get; set; } = string.Empty;

    /// <summary>小型貨物用区分</summary>
    public string FCOSYU4NO { get; set; } = string.Empty;

    /// <summary>軽自動車区分</summary>
    public string FCOSYUKEI { get; set; } = string.Empty;

    /// <summary>普通貨物（2t超）区分</summary>
    public string FCOSYU2TO { get; set; } = string.Empty;

    /// <summary>普通貨物（2t以下）区分</summary>
    public string FCOSYU2TU { get; set; } = string.Empty;

    /// <summary>普通貨物（車両重量8t超）区分</summary>
    public string FCOSYU8TO { get; set; } = string.Empty;

    /// <summary>特種区分</summary>
    public string FCOSYUKND { get; set; } = string.Empty;

    /// <summary>特殊区分</summary>
    public string FCOSYUSPL { get; set; } = string.Empty;

    /// <summary>乗合区分</summary>
    public string FCOSYUSHR { get; set; } = string.Empty;

    /// <summary>特殊用途</summary>
    public string FCOSYUYOT { get; set; } = string.Empty;

    /// <summary>車台番号</summary>
    public string FCOFRMNO { get; set; } = string.Empty;

    /// <summary>車両特定番号</summary>
    public string FCOFRMSERNO { get; set; } = string.Empty;

    /// <summary>登録予定日</summary>
    public string FCOTRKYTI { get; set; } = string.Empty;

    /// <summary>納車予定日</summary>
    public string FCONSYYTI { get; set; } = string.Empty;

    /// <summary>使用の本拠</summary>
    public string FCOBSEUSE0 { get; set; } = string.Empty;

    /// <summary>使用本拠の位置1</summary>
    public string FCOBSEUSE1 { get; set; } = string.Empty;

    /// <summary>使用本拠の位置2</summary>
    public string FCOBSEUSE2 { get; set; } = string.Empty;

    /// <summary>使用本拠の位置3</summary>
    public string FCOBSEUSE3 { get; set; } = string.Empty;

    /// <summary>正しい使用本拠の位置</summary>
    public string FCOBSEUSECRT { get; set; } = string.Empty;

    /// <summary>登録書類必着日</summary>
    public string FCODOCARV { get; set; } = string.Empty;

    /// <summary>AM/PM区分</summary>
    public string FCOAMPM { get; set; } = string.Empty;

    /// <summary>備考</summary>
    public string FCOBIKO { get; set; } = string.Empty;

    /// <summary>受注代行番号</summary>
    public string FCOJCHSUBNO { get; set; } = string.Empty;

    /// <summary>AM区分</summary>
    public string FCOAM { get; set; } = string.Empty;

    /// <summary>PM区分</summary>
    public string FCOPM { get; set; } = string.Empty;

    /// <summary>自動車の種類1</summary>
    public string FCOSYUNM1 { get; set; } = string.Empty;

    /// <summary>自動車の種類2</summary>
    public string FCOSYUNM2 { get; set; } = string.Empty;

    /// <summary>記入日（読取）</summary>
    public string FCOENTDAYREAD { get; set; } = string.Empty;

    /// <summary>OCR読取日</summary>
    public string FCOOCRDAY { get; set; } = string.Empty;

    /// <summary>OCR取込ファイル作成日時(更新日時)</summary>
    public DateTime FCOOCRDTTM { get; set; }

    /// <summary>取込日時</summary>
    public DateTime FCOIMPDT { get; set; }

    /// <summary>取込ステータス</summary>
    public int FCOSTS { get; set; }
}
