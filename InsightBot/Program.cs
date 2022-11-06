using Insight_bott;
using Quartz.Impl;
using Quartz;
using System.Threading;
using Telegram.Bot;
using Telegram.Bot.Types;
using Microsoft.EntityFrameworkCore;
using User = Insight_bott.User;



Insight_bott.Jobs.Sheduler sheduler = new Insight_bott.Jobs.Sheduler();
sheduler.Start();


Console.ReadLine();

