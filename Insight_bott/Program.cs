using Insight_bott;
using Quartz.Impl;
using Quartz;
using System.Threading;
using Telegram.Bot;
using Telegram.Bot.Types;

Insight_bott.Jobs.Sheduler sheduler = new Insight_bott.Jobs.Sheduler();
sheduler.Start();


Console.ReadLine();