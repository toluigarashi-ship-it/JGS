namespace DesktopApp.FrameSheetCheck;

/// <summary>
/// 車台番号連絡票確認画面の表示・編集用クラス
/// <para>
/// フォーム（FrameSheetCheckForm）とロジック（FrameSheetCheckLogic）間の
/// データ受け渡しに使用する
/// </para>
/// </summary>
internal sealed class FrameSheetCheckViewModel
{
    /// <summary>取込ID（主キー）</summary>
    public int FOCFCOID { get; set; }

    /// <summary>取込ファイル名</summary>
    public string? FOCIMPFILENM { get; set; }

    /// <summary>レコード番号（ヘッダー除く行数）</summary>
    public int? FOCIMPRCDCNT { get; set; }

    /// <summary>ファイル名</summary>
    public string? FOCFILENM { get; set; }

    /// <summary>帳票名</summary>
    public string? FOCFORMNM { get; set; }

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

    /// <summary>自家用</summary>
    public string? FOCSYUJI { get; set; }

    /// <summary>営業用</summary>
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

    /// <summary>車台番号</summary>
    public string? FOCFRMNO { get; set; }

    /// <summary>車両特定番号</summary>
    public string? FOCFRMSERNO { get; set; }

    /// <summary>登録予定日</summary>
    public string? FOCTRKYTI { get; set; }

    /// <summary>納車予定日</summary>
    public string? FOCNSYYTI { get; set; }

    /// <summary>使用の本拠</summary>
    public string? FOCBSEUSE0 { get; set; }

    /// <summary>使用本拠の位置1</summary>
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

    /// <summary>AM/PM_AM</summary>
    public string? FOCAM { get; set; }

    /// <summary>AM/PM_PM</summary>
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

    /// <summary>ステータス（TINYINT）</summary>
    public byte? FOCSTS { get; set; }

    /// <summary>顛末</summary>
    public byte? FOCTENMATSU { get; set; }

    // =========================
    // 変更フラグ
    // =========================

    /// <summary>契約No（発注時）変更フラグ（0:未変更 / 1:変更あり）</summary>
    public byte FOCKYKNOCHG { get; set; }

    /// <summary>車台番号 変更フラグ（0:未変更 / 1:変更あり）</summary>
    public byte FOCFRMNOCHG { get; set; }

    /// <summary>車両特定番号 変更フラグ（0:未変更 / 1:変更あり）</summary>
    public byte FOCFRMSERNOCHG { get; set; }

    /// <summary>販売店担当者 変更フラグ（0:未変更 / 1:変更あり）</summary>
    public byte FOCSTRTNTCHG { get; set; }

    /// <summary>自動車の種類（自家用/営業用）変更フラグ（0:未変更 / 1:変更あり）</summary>
    public byte FOCSYUJICHG { get; set; }

    /// <summary>自動車の種類（種類）変更フラグ（0:未変更 / 1:変更あり）</summary>
    public byte FOCSYUJOCHG { get; set; }

    /// <summary>登録予定日 変更フラグ（0:未変更 / 1:変更あり）</summary>
    public byte FOCTRKYTICHG { get; set; }

    /// <summary>納車予定日 変更フラグ（0:未変更 / 1:変更あり）</summary>
    public byte FOCNSYYTICHG { get; set; }

    /// <summary>都道府県 変更フラグ（0:未変更 / 1:変更あり）</summary>
    public byte FOCBSEUSE0CHG { get; set; }

    /// <summary>郵便番号 変更フラグ（0:未変更 / 1:変更あり）</summary>
    public byte FOCBSEUSE1CHG { get; set; }

    /// <summary>使用本拠の位置 変更フラグ（0:未変更 / 1:変更あり）</summary>
    public byte FOCBSEUSE2CHG { get; set; }

    /// <summary>登録書類必着日 変更フラグ（0:未変更 / 1:変更あり）</summary>
    public byte FOCDOCARVCHG { get; set; }

    // =========================
    // 監査情報
    // =========================

    /// <summary>確定日時</summary>
    public DateTime? FOCCNFDATE { get; set; }

    /// <summary>確定者（CHAR(6)）</summary>
    public string? FOCCNFUSR { get; set; }

    /// <summary>登録日時</summary>
    public DateTime? FOCINSDATE { get; set; }

    /// <summary>登録者（CHAR(6)）</summary>
    public string? FOCINSUSR { get; set; }

    /// <summary>更新日時</summary>
    public DateTime? FOCUPDDATE { get; set; }

    /// <summary>更新者（CHAR(6)）</summary>
    public string? FOCUPDUSR { get; set; }

    /// <summary>
    /// 顛末コンボボックス用のリスト
    /// </summary>
    public IReadOnlyList<Common.Db.JGSVNAMReader.NameItem> ComboItems { get; set; }
        = Array.Empty<Common.Db.JGSVNAMReader.NameItem>();

    // =========================
    // 表示制御（書類UT用）
    // =========================

    /// <summary>
    /// 書類UT用表示かどうか（true の場合、UT対象項目を強調表示）
    /// </summary>
    public bool IsDocumentUtMode { get; set; }

    /// <summary>
    /// 修正済み表示（入力欄の背景色変更）を行う対象かどうかを保持する
    /// <para>
    /// 値は確認テーブルの …CHG（0/1）から組み立てられる
    /// </para>
    /// </summary>
    public FrameSheetCheckChangedFlags Changed { get; set; } = new ();

    // =========================
    // 契約情報
    // =========================

    /// <summary>契約情報が存在するかどうか</summary>
    public bool IsKykinfoExists { get; set; }

    /// <summary>契約No</summary>
    public string? Kyk_LSKYKNO { get; set; }

    /// <summary>車名</summary>
    public string? Kyk_SHDNCARNM { get; set; }

    /// <summary>型式</summary>
    public string? Kyk_KATA { get; set; }

    /// <summary>担当部署コード</summary>
    public string? Kyk_EGYTNTBUCD { get; set; }

    /// <summary>担当スタッフ名</summary>
    public string? Kyk_EGYTNTNM { get; set; }

    /// <summary>担当チーム</summary>
    public string? Kyk_TRKTNTNM { get; set; }

    /// <summary>契約先名上段</summary>
    public string? Kyk_KYKSKNMJDN { get; set; }

    /// <summary>捺印書類送付先確定情報</summary>
    public string? Kyk_NTINCHK_DS { get; set; }

    /// <summary>発注先名上段（発注時）</summary>
    public string? Kyk_HCHSKNMJDN { get; set; }

    // =========================
    // 修正表示用
    // =========================

    /// <summary>修正済み表示用のフラグ群</summary>
    internal sealed class FrameSheetCheckChangedFlags
    {
        /// <summary>発注時契約Noが修正済みかどうか</summary>
        public bool FOCKYKNO { get; set; }

        /// <summary>車台番号が修正済みかどうか</summary>
        public bool FOCFRMNO { get; set; }

        /// <summary>車両特定番号が修正済みかどうか</summary>
        public bool FOCFRMSERNO { get; set; }

        /// <summary>販売店担当者が修正済みかどうか</summary>
        public bool FOCSTRTNT { get; set; }

        /// <summary>自動車の種類（自家用／営業用）が修正済みかどうか</summary>
        public bool FOCSYUJI { get; set; }

        /// <summary>自動車の種類（種類）が修正済みかどうか</summary>
        public bool FOCSYUJO { get; set; }

        /// <summary>登録予定日が修正済みかどうか</summary>
        public bool FOCTRKYTI { get; set; }

        /// <summary>納車予定日が修正済みかどうか</summary>
        public bool FOCNSYYTI { get; set; }

        /// <summary>使用の本拠（都道府県）が修正済みかどうか</summary>
        public bool FOCBSEUSE0 { get; set; }

        /// <summary>使用本拠の位置1が修正済みかどうか</summary>
        public bool FOCBSEUSE1 { get; set; }

        /// <summary>使用本拠の位置2が修正済みかどうか</summary>
        public bool FOCBSEUSE2 { get; set; }

        /// <summary>登録書類必着日が修正済みかどうか</summary>
        public bool FOCDOCARV { get; set; }
    }

}
