using ChatSystem.Client.Connection;
using ChatSystem.Client.Interfaces;
using ChatSystem.Client.Processors;
using ChatSystem.Client.Services;
using ChatSystem.Models;

using Microsoft.Extensions.DependencyInjection;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatSystem.Client.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddChatClient(this IServiceCollection services)
        {
            services.AddSingleton<IConnectionManager, WebSocketConnectionManager>();
            services.AddSingleton<IFileManager, FileManager>();
            services.AddSingleton<ICommandParser, CommandParser>();
            services.AddSingleton<IChatRoomManager, ChatRoomManager>();
            services.AddTransient<IMessageProcessor<ChatMessage>, ChatMessageProcessor>();
            services.AddTransient<IMessageProcessor<FileTransferMessage>, FileTransferProcessor>();
            services.AddTransient<IChatClient, ChatClient>();

            return services;
        }
    }
}
