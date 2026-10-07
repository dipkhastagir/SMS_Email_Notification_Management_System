namespace SMSNotificationSystem.DTOs
{
    public class DailyDeliveryDto
    {
        public DateTime Day { get; set; }
        public int SentCount { get; set; }
        public int FailedCount { get; set; }
    }
}
