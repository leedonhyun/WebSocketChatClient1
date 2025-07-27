using WebSocketChatClient1.Models;
using System.Threading.Tasks;

namespace WebSocketChatClient1.Interfaces;

public interface IMessageProcessor<T> where T : BaseMessage
{
    Task ProcessAsync(T message);
}