namespace Insight_bott.Logging;

// https://metanit.com/sharp/dotnet/1.3.php тут подробнее о том как работает

//var logger = new Logger(new SimpleLogService());
//logger.Log("Hello METANIT.COM");
 
//logger = new Logger(new GreenLogService());
//ogger.Log("Hello METANIT.COM");
 
interface ILogService
{
    void Write(string message);
}
// простой вывод на консоль
class SimpleLogService : ILogService
{
    // пишем лог в консоль
    public void Write(string message) => Console.WriteLine($"{DateTime.Now} {message}");
    
    
}
// сервис, который выводит сообщение зеленым цветом
class GreenLogService : ILogService
{
    public void Write(string message)
    {
        var defaultColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.DarkGreen;
        Console.WriteLine(message);
        Console.ForegroundColor = defaultColor;
    }
}
// тоже пока не понимаю для чего нужен при наличии dependency injection, все работет через SimpleLogService
class Logger
{
    ILogService logService;
    public Logger(ILogService logService) => this.logService = logService;
    public void Log(string message) =>logService.Write($"{DateTime.Now}  {message}");
}