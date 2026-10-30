namespace NAU_Web.Models
{
    public class FeeItemData
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Category { get; set; } = "be"; // "be", "bs", "mba", "short"
        public string Component { get; set; } = string.Empty;
        public string Amount { get; set; } = string.Empty;
        public string Frequency { get; set; } = "Per Semester";
        public string Note { get; set; } = string.Empty;
        public bool IsSubtotal { get; set; }
        public bool IsTotal { get; set; }
        public int DisplayOrder { get; set; }
    }

    public class FeeCardData
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Category { get; set; } = "be";
        public string Title { get; set; } = string.Empty;
        public string Amount { get; set; } = string.Empty;
        public string Period { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;
        public string IconClass { get; set; } = "fas fa-university";
        public string BgColor { get; set; } = "#e8f0fe";
        public string TextColor { get; set; } = "#1a5fa6";
    }

    public class ScholarshipData
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Title { get; set; } = string.Empty;
        public string Subtitle { get; set; } = string.Empty;
        public string Percentage { get; set; } = string.Empty;
        public string PercentageLabel { get; set; } = "Fee waiver on tuition";
        public List<string> Criteria { get; set; } = new List<string>();
        public string BadgeText { get; set; } = string.Empty;
        public string BadgeBg { get; set; } = "#e8f0fe";
        public string BadgeColor { get; set; } = "#1a5fa6";
        public string HeaderGradient { get; set; } = "linear-gradient(135deg,#0b2c4d,#1a5fa6)";
    }

    public class BankData
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Name { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public string TagText { get; set; } = "Accepted";
        public string TagBg { get; set; } = "#e8f0fe";
        public string TagColor { get; set; } = "#1a5fa6";
        public string LogoText { get; set; } = "BANK";
        public string LogoBgHex { get; set; } = "#003087";
        public string LogoUrl { get; set; } = string.Empty;
    }

    public class FinancialPageContent
    {
        public string FeeSectionLabel { get; set; } = "FEE STRUCTURE";
        public string FeeSectionTitle { get; set; } = "Tuition & Other Fees";
        public string FeeSectionDesc { get; set; } = "Fees are charged per semester. Amounts shown are for the 2026 academic year and subject to revision annually. All amounts are in Pakistani Rupees (PKR).";
        public string FeeNoticeText { get; set; } = "Fees must be paid within the first two weeks of each semester. Late payment may attract a surcharge of PKR 500 per week. CAA employees and nominees may be covered under their respective departmental training budgets.";
        
        public string TabLabelBe { get; set; } = "BE Programs";
        public string TabLabelBs { get; set; } = "BS Programs";
        public string TabLabelMba { get; set; } = "MBA";
        public string TabLabelShort { get; set; } = "Short Courses";

        public string ScholarshipSectionLabel { get; set; } = "SCHOLARSHIPS";
        public string ScholarshipSectionTitle { get; set; } = "Financial Aid & Scholarships";
        public string ScholarshipSectionDesc { get; set; } = "NAU offers merit and need-based scholarship programs to ensure that financial constraints do not prevent talented students from pursuing an aviation career.";
        public string ScholarshipNoticeText { get; set; } = "Scholarship applications must be submitted within the first three weeks of each semester. Late applications will not be entertained. Maintaining the scholarship requires fulfilling academic and conduct requirements each semester.";

        public string PaymentSectionLabel { get; set; } = "PAYMENT METHODS";
        public string PaymentSectionTitle { get; set; } = "How to Pay Your Fees";
        public string PaymentSectionDesc { get; set; } = "NAU accepts fee payments through multiple channels for your convenience. Always keep your payment receipt safe — it is your official proof of payment.";
        public string PaymentSubTitle { get; set; } = "Accepted Banks & Payment Options";
        
        public List<FeeItemData> FeeItems { get; set; } = new List<FeeItemData>();
        public List<FeeCardData> FeeCards { get; set; } = new List<FeeCardData>();
        public List<ScholarshipData> Scholarships { get; set; } = new List<ScholarshipData>();
        public List<BankData> Banks { get; set; } = new List<BankData>();

        public string GetTabLabel(string cat)
        {
            return cat switch
            {
                "be" => string.IsNullOrWhiteSpace(TabLabelBe) ? "BE Programs" : TabLabelBe,
                "bs" => string.IsNullOrWhiteSpace(TabLabelBs) ? "BS Programs" : TabLabelBs,
                "mba" => string.IsNullOrWhiteSpace(TabLabelMba) ? "MBA" : TabLabelMba,
                "short" => string.IsNullOrWhiteSpace(TabLabelShort) ? "Short Courses" : TabLabelShort,
                _ => cat.ToUpper()
            };
        }
    }
}
