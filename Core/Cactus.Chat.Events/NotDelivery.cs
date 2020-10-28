namespace Cactus.Chat.Events
{
    /// <summary>
    /// Delivery failure message means that server are not able to deliver a Message to Addressee right now. 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class NotDelivery<T> where T : IMessageIdentifier
    {
        /// <summary>
        /// A message that could not be delivered
        /// </summary>
        public T Message { get; set; }

        /// <summary>
        /// A user id that the message could not be delivered to.
        /// </summary>
        public string Addressee { get; set; }
    }
}