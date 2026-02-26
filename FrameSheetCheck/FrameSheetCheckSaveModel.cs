namespace DesktopApp.FrameSheetCheck;

/// <summary>
/// 車台番号連絡票CSV取込テーブル確認（OCR)（JGSTFRMCSVOCRCHK）への保存用モデル
/// <para>
/// INSERT / UPDATE の両方で共通利用する
/// </para>
/// </summary>
internal sealed class FrameSheetCheckSaveModel
{
    // =========================
    // 基本情報（取込情報）
    // =========================

    /// <summary>取込ID（取込テーブル FCOID）</summary>
    public int FOCFCOID { get; set; }

    /// <summary>取込ファイル名</summary>
    public string? FOCIMPFILENM { get; set; }

    /// <summary>レコード番号（ヘッダー除く行数）</summary>
    public int? FOCIMPRCDCNT { get; set; }

    /// <summary>ファイル名</summary>
    public string? FOCFILENM { get; set; }

    /// <summary>帳票名</summary>
    public string? FOCFORMNM { get; set; }

    // =========================
    // 販売店・契約情報
    // =========================

    /// <summary>販売店名1</summary>
    public string? FOCSTRNM1 { get; set; }

    /// <summary>販売店名2</summary>
    public string? FOCSTRNM2 { get; set; }

    /// <summary>販売店営業所名</summary>
    public string? FOCSTROFSNM { get; set; }

    /// <summary>販売店郵便番号</summary>
    public string? FOCSTRPSTNO { get; set; }

    /// <summary>販売店住所1</summary>
    public string? FOCSTRADD1 { get; set; }

    /// <summary>販売店住所2</summary>
    public string? FOCSTRADD2 { get; set; }

    /// <summary>販売店電話番号</summary>
    public string? FOCSTRTEL { get; set; }

    /// <summary>契約No（発注時）</summary>
    public string? FOCKYKNO { get; set; }

    /// <summary>車名</summary>
    public string? FOCCARNM { get; set; }

    /// <summary>型式</summary>
    public string? FOCKATA { get; set; }

    /// <summary>TMS部署名</summary>
    public string? FOCBUNM { get; set; }

    /// <summary>TMS担当スタッフ</summary>
    public string? FOCTNTSTFNM { get; set; }

    /// <summary>お客様名1</summary>
    public string? FOCKKYKNM1 { get; set; }

    /// <summary>お客様名2</summary>
    public string? FOCKKYKNM2 { get; set; }

    /// <summary>OSS申請</summary>
    public string? FOCOSS { get; set; }

    /// <summary>販売店担当者</summary>
    public string? FOCSTRTNT { get; set; }

    /// <summary>販売店e-mail</summary>
    public string? FOCSTREML { get; set; }

    /// <summary>記入日</summary>
    public string? FOCENTDAY { get; set; }

    // =========================
    // 自動車区分（0/1）
    // =========================

    /// <summary>自家用（0/1）</summary>
    public string? FOCSYUJI { get; set; }

    /// <summary>営業用（0/1）</summary>
    public string? FOCSYUEI { get; set; }

    /// <summary>乗用</summary>
    public string? FOCSYUJO { get; set; }

    /// <summary>小型貨物用（4NO.）</summary>
    public string? FOCSYU4NO { get; set; }

    /// <summary>軽（対）</summary>
    public string? FOCSYUKEI { get; set; }

    /// <summary>普貨（2t超）</summary>
    public string? FOCSYU2TO { get; set; }

    /// <summary>普貨（2t以下）</summary>
    public string? FOCSYU2TU { get; set; }

    /// <summary>普貨（車両重量8t超）</summary>
    public string? FOCSYU8TO { get; set; }

    /// <summary>特種</summary>
    public string? FOCSYUKND { get; set; }

    /// <summary>特殊</summary>
    public string? FOCSYUSPL { get; set; }

    /// <summary>乗合</summary>
    public string? FOCSYUSHR { get; set; }

    /// <summary>特殊用途</summary>
    public string? FOCSYUYOT { get; set; }

    // =========================
    // 車両・登録情報
    // =========================

    /// <summary>車台番号</summary>
    public string? FOCFRMNO { get; set; }

    /// <summary>車両特定番号</summary>
    public string? FOCFRMSERNO { get; set; }

    /// <summary>登録予定日</summary>
    public string? FOCTRKYTI { get; set; }

    /// <summary>納車予定日</summary>
    public string? FOCNSYYTI { get; set; }

    /// <summary>使用の本拠（都道府県）</summary>
    public string? FOCBSEUSE0 { get; set; }

    /// <summary>使用本拠の位置1（郵便番号等）</summary>
    public string? FOCBSEUSE1 { get; set; }

    /// <summary>使用本拠の位置2</summary>
    public string? FOCBSEUSE2 { get; set; }

    /// <summary>使用本拠の位置3</summary>
    public string? FOCBSEUSE3 { get; set; }

    /// <summary>正しい使用本拠の位置</summary>
    public string? FOCBSEUSECRT { get; set; }

    /// <summary>登録書類必着日</summary>
    public string? FOCDOCARV { get; set; }

    /// <summary>AM/PM</summary>
    public string? FOCAMPM { get; set; }

    /// <summary>備考</summary>
    public string? FOCBIKO { get; set; }

    /// <summary>受注代行No</summary>
    public string? FOCJCHSUBNO { get; set; }

    /// <summary>AM</summary>
    public string? FOCAM { get; set; }

    /// <summary>PM</summary>
    public string? FOCPM { get; set; }

    /// <summary>自動車の種類1</summary>
    public string? FOCSYUNM1 { get; set; }

    /// <summary>自動車の種類2</summary>
    public string? FOCSYUNM2 { get; set; }

    /// <summary>記入日（読取）</summary>
    public string? FOCENTDAYREAD { get; set; }

    /// <summary>OCR読取日</summary>
    public string? FOCOCRDAY { get; set; }

    /// <summary>OCR取込ファイル作成日時(更新日時)</summary>
    public DateTime? FOCOCRDTTM { get; set; }

    /// <summary>取込日時</summary>
    public DateTime? FOCIMPDT { get; set; }

    /// <summary>TMT納車時間</summary>
    public string? FOCTMTNSY { get; set; }

    /// <summary>TMT引取先(回送先)</summary>
    public string? FOCTMTPUP { get; set; }

    /// <summary>確認(契約番号)</summary>
    public byte FOCKYKNOCNF { get; set; }

    /// <summary>離島(都道府県)</summary>
    public byte FOCISLAND { get; set; }

    /// <summary>要確認(登録書類必着日)</summary>
    public byte FOCDOCARVCNF { get; set; }

    /// <summary>メモ1</summary>
    public string? FOCMEMO01 { get; set; }

    /// <summary>メモ2</summary>
    public string? FOCMEMO02 { get; set; }

    /// <summary>メモ3</summary>
    public string? FOCMEMO03 { get; set; }

    /// <summary>メモ4</summary>
    public string? FOCMEMO04 { get; set; }

    /// <summary>ステータス</summary>
    public int FOCSTS { get; set; }

    /// <summary>顛末</summary>
    public byte? FOCTENMATSU { get; set; }

    // =========================
    // 変更フラグ（0:未変更 / 1:変更あり）
    // =========================

    /// <summary>契約No（発注時）変更フラグ</summary>
    public int FOCKYKNOCHG { get; set; }

    /// <summary>車台番号変更フラグ</summary>
    public int FOCFRMNOCHG { get; set; }

    /// <summary>車両特定番号変更フラグ</summary>
    public int FOCFRMSERNOCHG { get; set; }

    /// <summary>販売店担当者変更フラグ</summary>
    public int FOCSTRTNTCHG { get; set; }

    /// <summary>自動車の種類(自家用/営業用)変更フラグ</summary>
    public int FOCSYUJICHG { get; set; }

    /// <summary>自動車の種類(種類)変更フラグ</summary>
    public int FOCSYUJOCHG { get; set; }

    /// <summary>登録予定日変更フラグ</summary>
    public int FOCTRKYTICHG { get; set; }

    /// <summary>納車予定日変更フラグ</summary>
    public int FOCNSYYTICHG { get; set; }

    /// <summary>都道府県変更フラグ</summary>
    public int FOCBSEUSE0CHG { get; set; }

    /// <summary>郵便番号変更フラグ</summary>
    public int FOCBSEUSE1CHG { get; set; }

    /// <summary>使用本拠の位置変更フラグ</summary>
    public int FOCBSEUSE2CHG { get; set; }

    /// <summary>登録書類必着日変更フラグ</summary>
    public int FOCDOCARVCHG { get; set; }

    // =========================
    // 確定・監査情報
    // =========================

    /// <summary>確定日時</summary>
    public DateTime? FOCCNFDATE { get; set; }

    /// <summary>確定者</summary>
    public string? FOCCNFUSR { get; set; }

    /// <summary>登録日時</summary>
    public DateTime? FOCINSDATE { get; set; }

    /// <summary>登録者</summary>
    public string? FOCINSUSR { get; set; }

    /// <summary>更新日時</summary>
    public DateTime? FOCUPDDATE { get; set; }

    /// <summary>更新者</summary>
    public string? FOCUPDUSR { get; set; }

    /// <summary>
    /// 楽観ロック用：画面表示時点の更新日時
    /// </summary>
    public DateTime? OldUpdDate { get; set; }
}