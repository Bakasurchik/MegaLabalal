using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using Client.Data;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using System.Linq;
//{0-8}{9-1023}
namespace Client
{
    public class ServerService : BackgroundService
    {

        struct msg_to_serv
        {
            public string guid;
            public string msgtype;
            public string msg;

            public msg_to_serv(string Guid, string Msgtype, string Msg)
            {
                guid = Guid;
                msgtype = Msgtype;
                msg = Msg;

            }
        }


        private readonly ApplicationDbContext _DBContext;
        const int port = 4040;

        public ServerService(IServiceScopeFactory scopeFactory/*ApplicationDbContext context*/)
        {
            using (var scope = scopeFactory.CreateScope()) // this will use `IServiceScopeFactory` internally
            {
                var context = scope.ServiceProvider.GetService<ApplicationDbContext>();
                _DBContext = context;
            }
        }


        //создание экземпляра сервера
        protected async override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            IPAddress localAddr = IPAddress.Parse("127.0.0.1");
            TcpListener listener = new TcpListener(localAddr, port);
            listener.Start();
            //прослушивание порта
            while (!stoppingToken.IsCancellationRequested)
            {
                TcpClient client = await listener.AcceptTcpClientAsync();
                Console.WriteLine("a new client connected");
                NetworkStream stream = client.GetStream();

                //обработка принятого запроса
                while (!stoppingToken.IsCancellationRequested)
                {
                    //data - то что пришло с запроса
                    byte[] data = new byte[1024];
                    int read = await stream.ReadAsync(data, 0, 1024, stoppingToken);
                    // если ничего не пришло, то прекращаем обработку запроса
                    if (data[0] == (byte)0)
                        break;
                    string cmd = Encoding.UTF8.GetString(data, 0, read);

                    string first;



                    if (cmd.Contains('!'))
                    {
                        string[] splited = cmd.Split("!");
                        if (splited[0] != "")
                        {
                            first = splited[0];
                            cmd = first;
                        }
                    }
                    //тут нужно реализовать логику обработки данных которые нам передали, распарсить их, понять когда нам передали guid, а когда собщение с данными датчика например


                    //разбиваем полученное сообщение на структуру

                    string[] recmsg = cmd.Split('|');

                    msg_to_serv msgstruct = new msg_to_serv(recmsg[0], recmsg[1], recmsg[2]);

                    Console.WriteLine($"received : {cmd}");

                    bool ff = IsExistingGUID(msgstruct.guid);
                    //обрабатываем типы сообщений
                    if (ff)
                    {
                        if (msgstruct.guid != "" && msgstruct.msgtype == "get_settings")
                        {
                            string settings = GetContSettings(msgstruct.guid);
                            settings += '\0';
                            string msg_to_send = settings;
                            await stream.WriteAsync(Encoding.UTF8.GetBytes(msg_to_send), 0, msg_to_send.Length);
                        }
                        long buf;
                        if (msgstruct.guid != "" && msgstruct.msgtype == "send_data" && long.TryParse(msgstruct.msg,out buf))
                        {
                            ControllerData cont_data = new ControllerData();
                            cont_data.Id = _DBContext.ControllersData.ToArray().Length.ToString();
                            cont_data.Guid = msgstruct.guid;
                            cont_data.Time = DateTime.Now;
                            cont_data.Value = Convert.ToInt64(msgstruct.msg);
                            _DBContext.ControllersData.Add(cont_data);
                            _DBContext.SaveChanges();
                        }
                    }
                    stream.Flush();

                }
            }

            throw new NotImplementedException();
        }
        //получить тип контроллера по гуиду
        //типы контроллеров : { temperature, lightning, humidity }
        public string GetControllerType(string guid)
        {
            return _DBContext.Controllers.Find(guid).Type;
        }

        //добавление контроллера в базу, по клиентскому запросу(кнопка добавить на сайте)
        public void AddNewControler(string guid, string type, string userlogin)
        {
            Controller cont = new Controller();
            cont.Id = guid;
            cont.Type = type;
            cont.User = userlogin;
            _DBContext.Controllers.Add(cont);
        }

        public bool IsExistingGUID(string guid)
        {
            //Controller cont = _DBContext.Controllers
            //                .Where(b => b.Id == guid)
            //                .FirstOrDefault();

            Controller cont = _DBContext.Controllers.Find(guid);

            if (cont != null)
                return true;
            else
                return false;
        }

        public string GetContSettings(string guid)
        {
            return _DBContext.Controllers.Find(guid).Settings;
        }

        
    }
}




