namespace SMSNotificationSystem.DTOs
{
    public class TriggerScanResult
    {
        public int TriggersEvaluated { get; set; }
        public int RecordsMatched { get; set; }
        public int MessagesQueued { get; set; }
        public int DuplicatesSkipped { get; set; }
        public int MissingContact { get; set; }

        public override string ToString() =>
            $"{TriggersEvaluated} trigger(s) evaluated, {RecordsMatched} record(s) matched, " +
            $"{MessagesQueued} queued, {DuplicatesSkipped} duplicate(s) skipped, {MissingContact} without contact";
    }

    public class DispatchSummary
    {
        public int Claimed { get; set; }
        public int Sent { get; set; }
        public int Retrying { get; set; }
        public int Failed { get; set; }

        public override string ToString() =>
            $"{Claimed} claimed, {Sent} sent, {Retrying} will retry, {Failed} failed";
    }
}
