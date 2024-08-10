using BackendTZ.Data;
using BackendTZ.Models;
using BackendTZ.Models.Dto;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using System.Drawing;
using System.Text;
using System.Text.RegularExpressions;

namespace BackendTZ.Controllers
{
    [Route("api/Experiment")]
    [ApiController]
    public class ExperimentController : ControllerBase
    {
        private readonly ILogger<ExperimentController> _logger;
        private ApplicationDbContext _db;
        public ExperimentController(ILogger<ExperimentController> logger, ApplicationDbContext db)   // используем внедрение зависимостей в конструкторе для работы с бд и логами в нашем контроллере
        {
            _logger = logger;
            _db = db;
        }

        [HttpGet("ButtonColor/{devicetoken}")]   // создаём первую конечную точку типа HttpGet для первого эксперемента

        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult<UserDto> GetColor(int devicetoken)
        {
            var number = _db.Users.FirstOrDefault(u => u.DeviceToken == devicetoken); // проверяем есть ли юзер в бд

            if (number == null)      // логика работы с новым юзером 
            {
                string color;
                Random random = new Random();
                int randomNumber = random.Next(1, 100);

                if (randomNumber < 33)
                {
                    color = "#FF0000";
                }
                else if (randomNumber < 66)
                {
                    color = "#00FF00";
                }
                else
                {
                    color = "#0000FF";
                }

                using (var transaction = _db.Database.BeginTransaction())
                {
                    try
                    {
                        _db.Database.ExecuteSqlRaw("SET IDENTITY_INSERT Users ON");

                        User model = new()
                        {
                            DeviceToken = devicetoken,
                            Key = "button_color",
                            Option = color,
                            CreatedDate = DateTime.Now
                        };
                        _db.Users.Add(model);
                        _db.SaveChanges();

                        _db.Database.ExecuteSqlRaw("SET IDENTITY_INSERT Users OFF");

                        transaction.Commit();

                        UserDto modelDto = new()
                        {
                            Key = model.Key,
                            Option = model.Option
                        };
                        _logger.LogInformation($"creating a new user (device token = {devicetoken}, experiment = button_color)");
                        return Ok(modelDto);
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return BadRequest("error executing transaction (experiment = button_color)");
                    }
                   
                }
                
            }

            var experiment = _db.Users
                .Where(u => u.DeviceToken == devicetoken)
                .Select(g => new
                {
                    Key = g.Key,
                    Option = g.Option
                })
                .FirstOrDefault();

            if (experiment.Key == "button_color")     // логика работы с юзером этого эксперемента 
            {
                UserDto modelDto = new()
                {
                    Key = experiment.Key,
                    Option = experiment.Option
                };
                _logger.LogInformation($"user (device token = {devicetoken}) from button_color experiment ");
                return Ok(modelDto);
            }

            _logger.LogError(" user not from button_color experiment");
            return BadRequest();                                 // ответ если юзер уже участвует в другом эксперименте
        }


        [HttpGet("ButtonPrice/{devicetoken}")]        // создаём вторую конечную точку типа HttpGet для второго эксперемента

        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]

        public ActionResult<UserDto> GetPrice (int devicetoken)
        {
            var number = _db.Users.FirstOrDefault(u => u.DeviceToken == devicetoken);

            if (number == null)             // логика работы с новым юзером 
            {
                string price;
                Random random = new Random();
                int randomNumber = random.Next(1, 100);

                if(randomNumber < 75)
                {
                    price = "10";
                }
                else if (randomNumber < 85)
                {
                    price = "20";
                }
                else if (randomNumber < 90)
                {
                    price = "50";
                }
                else
                {
                    price = "5";
                }

                using (var transaction = _db.Database.BeginTransaction())
                {
                    try
                    {
                        _db.Database.ExecuteSqlRaw("SET IDENTITY_INSERT Users ON");

                        User model = new()
                        {
                            DeviceToken = devicetoken,
                            Key = "price",
                            Option = price,
                            CreatedDate = DateTime.Now
                        };
                        _db.Users.Add(model);
                        _db.SaveChanges();

                        _db.Database.ExecuteSqlRaw("SET IDENTITY_INSERT Users OFF");

                        transaction.Commit();

                        UserDto modelDto = new()
                        {
                            Key = model.Key,
                            Option = model.Option
                        };
                        _logger.LogInformation($"creating a new user (device token = {devicetoken}, experiment = price)");
                        return Ok(modelDto);
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return BadRequest("error executing transaction (experiment = price)");
                    }

                }
            }

            var experiment = _db.Users
                .Where(u => u.DeviceToken == devicetoken)
                .Select(g => new
                 {
                     Key = g.Key,
                     Option = g.Option
                 })
                .FirstOrDefault();

            if (experiment.Key == "price")     // логика работы с юзером этого эксперемента 
            {
                UserDto modelDto = new()
                {
                    Key = experiment.Key,
                    Option = experiment.Option
                };
                _logger.LogInformation($"user (device token = {devicetoken}) from price experiment ");
                return Ok(modelDto);
            }

            _logger.LogError(" user not from price experiment");
            return BadRequest();                                 // ответ если юзер уже участвует в другом эксперименте
        }

        [HttpGet("GetStatistics")]     // создаём третью  конечную точку типа HttpGet для обработки статистики 

        public IActionResult GetStatistics()
        {
            var htmlBuilder = new StringBuilder();
            htmlBuilder.Append("<html><body><h1>Statistics</h1>");
            htmlBuilder.Append("<html><body><h2>button_color</h2>");
            htmlBuilder.Append("</table></body></html>");
            htmlBuilder.Append("<html><body>");
            htmlBuilder.Append("<table border='1'><tr>");
            htmlBuilder.Append("<th style='width:100px;'></th>" +
                               "<th style='width:100px;'>SumDevices</th>" +
                               "<th style='width:100px;'>#0000FF</th>" +
                               "<th style='width:100px;'>#00FF00</th>" +
                               "<th style='width:100px;'>#FF0000</th></tr>");

            var query = _db.Users     // формируем запрос с помощью linq на заполнение первой таблицы
                .Where(u => u.Key == "button_color")
                .GroupBy(u => u.Key)
                .Select(g => new
                {
                    Key = g.Key,
                    Sum = g.Count(),
                    Count0000FF = _db.Users
                        .Where(u => u.Option == "#0000FF")
                        .Count(),
                    Count00FF00 = _db.Users
                        .Where(u => u.Option == "#00FF00")
                        .Count(),
                    CountFF0000 = _db.Users
                        .Where(u => u.Option == "#FF0000")
                        .Count()
                })
                .FirstOrDefault();

            htmlBuilder.Append("<html><body>");
            htmlBuilder.Append("<table border='1'><tr>");
            htmlBuilder.Append($"<th style='width:100px;'>users</th>"+
                               $"<th style='width:100px;'>{query.Sum}</th>" +
                               $"<th style='width:100px;'>{query.Count0000FF}</th>" +
                               $"<th style='width:100px;'>{query.Count00FF00}</th>" +
                               $"<th style='width:100px;'>{query.CountFF0000}</th></tr>");
            htmlBuilder.Append("</table></body></html>");

            htmlBuilder.Append("<div style= 'margin-top: 15px;' ></div>");
            htmlBuilder.Append("<html><body><h2>price</h2>");
            htmlBuilder.Append("</table></body></html>");
            htmlBuilder.Append("<html><body>");
            htmlBuilder.Append("<table border='1'><tr>");
            htmlBuilder.Append("<th style='width:100px;'></th>" +
                               "<th style='width:100px;'>SumDevices</th>" +
                               "<th style='width:100px;'>price = 10</th>" +
                               "<th style='width:100px;'>price = 20</th>" +
                               "<th style='width:100px;'>price = 50</th>" +
                               "<th style='width:100px;'>price = 5</th></tr>");

            var query1 = _db.Users    // формируем запрос с помощью linq на заполнение второй таблицы
                .Where(u => u.Key == "price")
                .GroupBy(u => u.Key)
                .Select(g => new
                {
                    Key = g.Key,
                    Sum = g.Count(),
                    Count10 = _db.Users
                        .Where(u => u.Option == "10")
                        .Count(),
                    Count20 = _db.Users
                        .Where(u => u.Option == "20")
                        .Count(),
                    Count50 = _db.Users
                        .Where(u => u.Option == "50")
                        .Count(),
                    Count5 = _db.Users
                        .Where(u => u.Option == "5")
                        .Count()
                })
                .FirstOrDefault();

            
            htmlBuilder.Append("<html><body>");
            htmlBuilder.Append("<table border='1'><tr>");
            htmlBuilder.Append("<th style='width:100px;'>users</th>" +
                               $"<th style='width:100px;'>{query1.Sum}</th>" +
                               $"<th style='width:100px;'>{query1.Count10}</th>" +
                               $"<th style='width:100px;'>{query1.Count20}</th>" +
                               $"<th style='width:100px;'>{query1.Count50}</th>" +
                               $"<th style='width:100px;'>{query1.Count5}</th></tr>");
            htmlBuilder.Append("</table></body></html>");

            var contentResult = new ContentResult
            {
                Content = htmlBuilder.ToString(),
                ContentType = "text/html",
                StatusCode = 200
            };

            _logger.LogInformation("statistics request completed ");

            return contentResult;
        }

    }
}
