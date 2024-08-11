<a id = "top"></a>
# BackendTZ
Этот проект представляет собой RESTful API, разработанный с использованием Entity Framework Core, используя подход  "Code First". API работает через swagger, предоставляет доступ к данным из базы данных MS SQL, поддерживает создание, чтение, обновление и удаление (CRUD) операций.
## Что делает проект 
BackendTZ разработан для выполнения двух экспериментов компании. В одном эксперименте изменяется цвет кнопки на сайте у пользователя, за этот эксперимент отвечает конечная точка __[HttpGet("ButtonColor/{devicetoken}")]__, в другом  - изменение цены для пользователя, за этот эксперимент отвечает конечная точка __[HttpGet("ButtonPrice/{devicetoken}")]__. 

## Содержание 

-  [Краткое описание работы проекта](#one)
-  [Работа конечной точки на примере [HttpGet("ButtonColor/{devicetoken}")]](#two)
-  [Страничка анализа экспериментов](#three)
-  [Использование MS SQL бд](#four)
-  [Особенности некоторых решений проекта](#five)


<a id = "one"></a>
## Краткое описание работы проекта  
User при запросе к API проекта, генерирует уникальный device-token. Этот device-token обрабатывается на одной из конечных точек проверяя :
1. Новый ли это user - записывает его в бд, прикрепляя его к этому эксперименту и выдаёт одну из опций эксперимента.
   
   _если user уже записан в бд, то проводится вторая проверка:_
   
2. Является ли user участником этого эксперимента - выдаётся название эксперимента и опция которую user получил изначально.
   
Если проверка первого и второго пункта провалилась, то выдаётся ошибка, ведь user участвует в другом эксперименте и не должен участвовать в этом. 


<a id = "two"></a>
## Работа конечной точки на примере [HttpGet("ButtonColor/{devicetoken}")]  

Новый user, запрос через swagger: 

![image](https://github.com/user-attachments/assets/d8c289fd-b835-4ece-9cc2-473e1243e28f)

Запрос выполнен удачно и вернулся код 200 c названием эксперимента и опцией для этого user

О том что это именно новый user мы можем убедиться просмотрев логи 

![image](https://github.com/user-attachments/assets/193a3ce6-7e37-4b91-9189-c1847c32900c)

### Как это работает ?

В начале мы проверяем есть ли user в бд 

```
var number = _db.Users.FirstOrDefault(u => u.DeviceToken == devicetoken);
```

Затем (так как его нет в бд) рандомно присваиваем ему значение опции (в пропорциях указанных в тз), название этого эксперимента и сохраняем всё в бд. в этом.

```
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
        _logger.LogError("error executing transaction (experiment = button_color)");
        return BadRequest();
    }
   
}
```
Такой же запрос мы могли бы сделать напрямую из командной строки 

![image](https://github.com/user-attachments/assets/66e72f3c-681e-4005-b574-3b53fd40c8a1)

Но во втором случае запрос для user с device-token = 589, будет иметь другую логику выполнения ведь такой юзер уже находиться в бд. 

Мы так же это можем проверить посмотрев логи 

![image](https://github.com/user-attachments/assets/d1841cb2-1cb5-4cb0-b823-d5c191a357c3)

В этом случае мы проверяем находится ли юзер в нашем эксперименте. 

Для этого делаем linq-запрос в бд чтобы узнать инфу о user  

```
var experiment = _db.Users
    .Where(u => u.DeviceToken == devicetoken)
    .Select(g => new
    {
        Key = g.Key,
        Option = g.Option
    })
    .FirstOrDefault();
```

И исходя из ответа либо выдаём информацию о нём : 

```
 if (experiment.Key == "button_color")     
 {
     UserDto modelDto = new()
     {
         Key = experiment.Key,
         Option = experiment.Option
     };
     _logger.LogInformation($"user (device token = {devicetoken}) from button_color experiment ");
     return Ok(modelDto);
 }
```

либо если user делает get-запрос и не находится в этом эксперименте - выдаём ошибку : 

```
_logger.LogError(" user not from button_color experiment");
return BadRequest(); 
```

У второй конечной точки схожая логика обработки запросов.


<a id = "three"></a>
## Страничка анализа экспериментов  [(вверх)](#top)

В проекте реализована html-страничка анализа экспериментов. Она так же является HttpGet запросом.

![image](https://github.com/user-attachments/assets/8eeca45d-b3ad-4805-94b2-b933e4d01d3c)

перейдя по : 

```
https://localhost:7023/api/Experiment/GetStatistics
```

можно увидеть небольшой анализ выполнения экспериментов 

![image](https://github.com/user-attachments/assets/9042217e-3dba-4777-a14d-d8326825cca8)

<a id = "four"></a>
## Использование MS SQL бд  [(вверх)](#top)

В проекте используется ms sql база данных, для работы с которой были установлены NuGet пакеты: 

- Microsoft.EntityFrameworkCore.SqlServer
- Microsoft.EntityFrameworkCore.Tools

Управление сущностями хранящимися в бд будет совершаться с помощью включённого функционала специального класса  DbContext. Для этого в проекте был создан дочерний класс от DbContext, где в свою очередь была создана предустановка для хранения определённого типа сущностей: 

```
public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)  
    : base(options)
{ 

}
```

Так же произведена настройка подключения к бд на локальном пк в файле appsettings.json 

```
 "Logging": {
   "LogLevel": {
     "Default": "Information",
     "Microsoft.AspNetCore": "Warning"
   }
 },
 "AllowedHosts": "*",
 "ConnectionStrings": { 
   "DefaultSQLConnection": "Server = DESKTOP-F34EHM9; Database = UsersAPI; Trusted_Connection = True; MultipleActiveResultSets = true; Encrypt = False"
 }
```

Так же для корректной работы бд в нашем приложении, в Program.cs связываем настройки из appsettings.json с ApplicationDbContext,это делается чтобы к бд можно было обращаться из различных частей нашего приложения :

```
builder.Services.AddDbContext<ApplicationDbContext>(option => 
{
    option.UseSqlServer(builder.Configuration.GetConnectionString("DefaultSQLConnection"));
});
```

__Важно !__ Все изменения которые мы делаем в нашем приложении и которые касаются  изменений в бд , я обновлял с помощью миграций 


Была создана простая структура бд : 

![image](https://github.com/user-attachments/assets/c21a7a77-fb66-41e2-aad2-4299a4bde557)

Её отображение непосредственно в sql :

![image](https://github.com/user-attachments/assets/2ab6399e-4a07-49bb-a47e-dcff0c25bfa3)

<a id = "five"></a>
## Особенности некоторых решений проекта  [(вверх)](#top)

1. Если пользователь не участвует в эксперименте, но сделал запрос , то ему выдаётся ошибка.

Эту задачу можно было выполнить разными подходами. Самый логичный - это если пользователь не должен участвовать в эксперименте - выдавать ему значение по дефолту. Но так как в установленном тз не указывалось значение по дефолту, я предполагаю, что есть некая логика обработки ошибок у клиента и вывода значений по дефолту.

2. Более сложная структура бд.

При реализации структуры бд можно было сделать её более сложнее. Состоящую из нескольких таблиц : таблицу в которой хранились бы все сессии user, так же таблицу с хранящимися в ней экспериментами и таблицу с результатом экспериментов . Это позволило бы проводить некую масштабируемость в будущем, но если взглянуть на это с другой стороны, в данном проекте речь идёт о реализации двух простых экспериментов с анализом их выполнения. Я не считаю логичным добавления сюда сложной структуры бд, реализация которой увеличила бы время на реализацию данного эксперимента, что потенциально могло бы повлечь негативные последствия.    

## Автор проекта 

Алянчиков Александр 

