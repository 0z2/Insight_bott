using System.Text;

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
    public void Write(string message)
    {
        // строка для записи
        string text = $"{DateTime.Now} {message}\n"; 
        
        //пишем в консоль
        Console.Write(text);
        
        # region [ WriteLoggsToFile ]
        string path = "/Users/betehtin/Yandex.Disk.localized/Программирование/C#/repos/Insight_bott/logs.txt";   // путь к файлу
        // запись в файл
        using (FileStream fstream = new FileStream(path, FileMode.Append))
        {
            // преобразуем строку в байты
            byte[] buffer = Encoding.Default.GetBytes(text);
            // запись массива байтов в файл
            fstream.WriteAsync(buffer, 0, buffer.Length);
            //Console.WriteLine("Текст записан в файл");
        }
        # endregion
    }
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

