namespace Prima.Queue
{
    public class EventSlotState
    {
        public string EventId { get; set; }

        public bool Confirmed { get; set; }

        public EventSlotState(string eventId)
        {
            EventId = eventId;
        }

        public static implicit operator EventSlotState(string eventId) => new EventSlotState(eventId);
    }
}