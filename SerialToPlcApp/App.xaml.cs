using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using SerialToPlcApp.Configuration;
using SerialToPlcApp.DataProcessing;
using SerialToPlcApp.Queues;
using SerialToPlcApp.Services;
using SerialToPlcApp.Models;

namespace SerialToPlcApp
{

    public partial class App : Application
    {
    }
}
