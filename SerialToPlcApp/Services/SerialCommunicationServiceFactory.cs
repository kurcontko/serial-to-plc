using Microsoft.Extensions.DependencyInjection;
using SerialToPlcApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerialToPlcApp.Services
{
    public interface ISerialCommunicationServiceFactory
    {
        ISerialCommunicationService Create(SerialSetting deviceSetting, List<SerialCommand> serialCommands);
    }

    public class SerialCommunicationServiceFactory : ISerialCommunicationServiceFactory
    {
        private readonly IServiceProvider serviceProvider;

        public SerialCommunicationServiceFactory(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public ISerialCommunicationService Create(SerialSetting deviceSetting, List<SerialCommand> serialCommands)
        {
            return ActivatorUtilities.CreateInstance<SerialCommunicationService>(serviceProvider, deviceSetting, serialCommands);
        }
    }
}
