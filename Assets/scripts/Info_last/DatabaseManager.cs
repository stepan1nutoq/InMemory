using UnityEngine;
using System.Collections.Generic;
using System.Data;
using Mono.Data.SqliteClient;
using System.IO;

public class DatabaseManager : MonoBehaviour
{
    private static DatabaseManager _instance;
    public static DatabaseManager Instance => _instance;
    
    // SQLite объекты (как в SQLiter ассете)
    private IDbConnection _connection;
    private IDbCommand _command;
    private IDataReader _reader;
    
    // Путь к базе данных
    private string _databasePath;
    private const string DATABASE_NAME = "HeroesDatabase";

    [Header("Настройки базы данных")]
    [SerializeField] private bool forceRecreateDatabase = false;
    
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Проверяем, нужно ли пересоздать базу данных
        if (forceRecreateDatabase)
        {
            DeleteDatabase();
        }
        
        // Инициализируем базу данных
        InitializeDatabase();
    }

    public void DeleteDatabase()
    {
        try
        {
            // Закрываем соединение если оно открыто
            if (_connection != null && _connection.State == ConnectionState.Open)
            {
                _connection.Close();
                _connection.Dispose();
                _connection = null;
            }
            
            // Находим файл базы данных
            string dbFilePath = Path.Combine(Application.dataPath, "..", DATABASE_NAME + ".db");
            
            // Для редактора Unity
            if (Application.isEditor)
            {
                dbFilePath = Path.Combine(Directory.GetCurrentDirectory(), DATABASE_NAME + ".db");
            }
            // Для билда
            else
            {
                dbFilePath = Path.Combine(Application.persistentDataPath, DATABASE_NAME + ".db");
            }
            
            // Пробуем несколько путей
            string[] possiblePaths = new string[]
            {
                dbFilePath,
                Path.Combine(Application.dataPath, DATABASE_NAME + ".db"),
                Path.Combine(Application.streamingAssetsPath, DATABASE_NAME + ".db"),
                Path.Combine(Directory.GetCurrentDirectory(), DATABASE_NAME + ".db"),
                DATABASE_NAME + ".db" // Рядом с exe файлом
            };
            
            bool fileDeleted = false;
            
            foreach (string path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    try
                    {
                        File.Delete(path);
                        Debug.Log($"Файл базы данных удален: {path}");
                        fileDeleted = true;
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"Не удалось удалить файл {path}: {e.Message}");
                    }
                }
            }
            
            if (!fileDeleted)
            {
                Debug.Log("Файл базы данных не найден, возможно он еще не создан");
            }
            
            // Также удаляем файл журнала если есть
            foreach (string path in possiblePaths)
            {
                string journalPath = path + "-journal";
                if (File.Exists(journalPath))
                {
                    try
                    {
                        File.Delete(journalPath);
                        Debug.Log($"Файл журнала удален: {journalPath}");
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"Не удалось удалить журнал {journalPath}: {e.Message}");
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Ошибка при удалении базы данных: {e.Message}");
        }
    }
    
    private void InitializeDatabase()
    {
        try
        {
            // Устанавливаем путь к базе данных
            // SQLiter создает базу в папке проекта (для редактора) или рядом с exe (для билда)
            _databasePath = "URI=file:" + DATABASE_NAME + ".db";
            
            Debug.Log("Инициализация базы данных: " + _databasePath);
            
            // Создаем подключение
            _connection = new SqliteConnection(_databasePath);
            _connection.Open();
            _command = _connection.CreateCommand();
            
            // Оптимизации для скорости (как в SQLiter)
            ExecuteSQL("PRAGMA journal_mode = WAL;");
            ExecuteSQL("PRAGMA synchronous = OFF");
            
            // Создаем таблицу героев
            CreateHeroesTable();
            
            // Проверяем и заполняем данные
            CheckAndPopulateData();
            
            Debug.Log("База данных успешно инициализирована");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Ошибка инициализации базы данных: {e.Message}\n{e.StackTrace}");
        }
    }
    
    private void CreateHeroesTable()
    {
        try
        {
            // Проверяем, существует ли таблица (как в SQLiter примере)
            _command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='Heroes'";
            _reader = _command.ExecuteReader();
            
            bool tableExists = _reader.Read();
            _reader.Close();
            
            if (!tableExists)
            {
                // Создаем таблицу
                string createTableSQL = @"
                    CREATE TABLE Heroes (
                        id INTEGER PRIMARY KEY,
                        name TEXT NOT NULL,
                        title TEXT NOT NULL,
                        description TEXT,
                        years TEXT,
                        rewards TEXT,
                        imageName TEXT,
                        nextScene TEXT,
                        is_unlocked INTEGER DEFAULT 1
                    )";
                
                ExecuteSQL(createTableSQL);
                Debug.Log("Таблица 'Heroes' создана");
            }
            else
            {
                Debug.Log("Таблица 'Heroes' уже существует");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Ошибка создания таблицы: {e.Message}");
        }
    }
    
    private void CheckAndPopulateData()
    {
        try
        {
            // Проверяем количество записей в таблице
            _command.CommandText = "SELECT COUNT(*) FROM Heroes";
            object result = _command.ExecuteScalar();
            
            int rowCount = 0;
            if (result != null)
            {
                rowCount = System.Convert.ToInt32(result);
            }
            
            if (rowCount == 0)
            {
                Debug.Log("Таблица пуста, заполняем данными...");
                InsertDefaultHeroes();
            }
            else
            {
                Debug.Log($"В таблице уже есть {rowCount} записей");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Ошибка проверки данных: {e.Message}");
        }
    }
    
    private void InsertDefaultHeroes()
    {
        try
        {
            // Начинаем транзакцию для быстрой вставки
            ExecuteSQL("BEGIN TRANSACTION");
            
            List<HeroData> heroes = GetDefaultHeroesList();
            
            foreach (var hero in heroes)
            {
                string insertSQL = $@"
                    INSERT INTO Heroes 
                    (id, name, title, description, years, rewards, imageName, nextScene, is_unlocked)
                    VALUES (
                        {hero.id},
                        '{EscapeString(hero.name)}',
                        '{EscapeString(hero.title)}',
                        '{EscapeString(hero.description)}',
                        '{EscapeString(hero.years)}',
                        '{EscapeString(hero.rewards)}',
                        '{EscapeString(hero.imageName)}',
                        '{EscapeString(hero.nextScene)}',
                        {(hero.is_unlocked ? 1 : 0)}
                    )";
                
                ExecuteSQL(insertSQL);
            }
            
            ExecuteSQL("COMMIT");
            Debug.Log($"Успешно добавлено {heroes.Count} героев");
        }
        catch (System.Exception e)
        {
            ExecuteSQL("ROLLBACK");
            Debug.LogError($"Ошибка при добавлении данных: {e.Message}");
        }
    }
    
    private string EscapeString(string input)
    {
        if (string.IsNullOrEmpty(input)) return "";
        // Экранируем апострофы для SQL
        return input.Replace("'", "''");
    }
    
    private void ExecuteSQL(string sql)
    {
        try
        {
            _command.CommandText = sql;
            _command.ExecuteNonQuery();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"SQL ошибка при выполнении: {sql}\nОшибка: {e.Message}");
        }
    }
    
    // Основной метод для получения героя по ID
    public HeroData GetHeroById(int heroId)
    {
        if (_connection == null || _connection.State != ConnectionState.Open)
        {
            Debug.LogError("Соединение с базой данных не открыто");
            return null;
        }
        
        try
        {
            string query = $"SELECT * FROM Heroes WHERE id = {heroId}";
            _command.CommandText = query;
            
            _reader = _command.ExecuteReader();
            
            if (_reader.Read())
            {
                HeroData hero = new HeroData
                {
                    id = _reader.GetInt32(0),
                    name = _reader.GetString(1),
                    title = _reader.GetString(2),
                    description = _reader.GetString(3),
                    years = _reader.GetString(4),
                    rewards = _reader.GetString(5),
                    imageName = _reader.GetString(6),
                    nextScene = _reader.GetString(7),
                    is_unlocked = _reader.GetInt32(8) == 1
                };
                
                _reader.Close();
                return hero;
            }
            
            _reader.Close();
            Debug.LogWarning($"Герой с ID {heroId} не найден в базе данных");
            return null;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Ошибка получения героя {heroId}: {e.Message}");
            
            if (_reader != null && !_reader.IsClosed)
                _reader.Close();
                
            return null;
        }
    }
    
    // Получить всех героев (для меню выбора)
    public List<HeroData> GetAllHeroes()
    {
        List<HeroData> heroes = new List<HeroData>();
        
        if (_connection == null || _connection.State != ConnectionState.Open)
        {
            Debug.LogError("Соединение с базой данных не открыто");
            return heroes;
        }
        
        try
        {
            _command.CommandText = "SELECT * FROM Heroes ORDER BY id";
            _reader = _command.ExecuteReader();
            
            while (_reader.Read())
            {
                heroes.Add(new HeroData
                {
                    id = _reader.GetInt32(0),
                    name = _reader.GetString(1),
                    title = _reader.GetString(2),
                    description = _reader.GetString(3),
                    years = _reader.GetString(4),
                    rewards = _reader.GetString(5),
                    imageName = _reader.GetString(6),
                    nextScene = _reader.GetString(7),
                    is_unlocked = _reader.GetInt32(8) == 1
                });
            }
            
            _reader.Close();
            Debug.Log($"Загружено {heroes.Count} героев из базы данных");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Ошибка получения всех героев: {e.Message}");
            
            if (_reader != null && !_reader.IsClosed)
                _reader.Close();
        }
        
        return heroes;
    }
    
    // Полный список героев с вашими данными
    private List<HeroData> GetDefaultHeroesList()
    {
        return new List<HeroData>
        {
            new HeroData
            {
                id = 1,
                name = "Nat_K",
                title = "Наталья Александровна Качуевская",
                description = "Наталья Спирова (Качуевская) родилась в 1922 году в Москве. Студенткой ГИТИСа в 1941 году возглавила фронтовую концертную бригаду, где познакомилась и вышла замуж за командира партизанского отряда Павла Качуевского. После его гибели добровольцем ушла на фронт санинструктором. 20 ноября 1942 года, во время контрнаступления под Сталинградом в калмыцких степях, она одна вступила в бой с вышедшей из окружения группой немецких солдат у блиндажа с ранеными. Отвлекая врага, Наталья убила несколько из них, а когда была окружена, подорвала себя и фашистов последней гранатой, ценой жизни спасла двадцать раненых бойцов. Была похоронена в калмыцких степях, позже её останки перенесены в братскую могилу в посёлке Яшкуль. Память о Наталье Качуевской увековечена в Москве — в её честь названа улица в Косино-Ухтомском районе (ВАО). Её подвигу также посвящены памятники в Измайловском парке и на Мамаевом кургане.",
                years = "1922-1942 гг.",
                rewards = "Звание Героя Россиийской Федерации за «мужество и героизм, проявленные в борьбе с немецко-фашистскими захватчиками в Великой Отечественной войне 1941—1945 годов» - посмертно.\nМедаль «За отвагу» - посмертно.",
                imageName = "Nat_K_img",
                nextScene = "Nat_K_Scene",
                is_unlocked = true
            },
            
            new HeroData
            {
                id = 2,
                name = "Tat_M",
                title = "Татьяна Петровна Макарова",
                description = "Татьяна Петровна Макарова — отважная советская лётчица, гвардии лейтенант, героиня Великой Отечественной войны. Выпускница Московского пищевого техникума и аэроклуба, в октябре 1941 года добровольцем вступила в женский авиационный полк под командованием Марины Расковой. После окончания Энгельсской военной авиашколы с мая 1942 года воевала в составе легендарного 588-го ночного бомбардировочного авиационного полка, прозванного немцами «ночными ведьмами». Командовала звеном, а затем эскадрильей. За время войны совершила 628 боевых ночных вылетов на тихоходном, но маневренном биплане По-2 (У-2), сбросив на позиции и объекты противника 96 тонн бомб. Участвовала в битвах за Кавказ и Кубань, освобождала Крым, Белоруссию и Польшу. Погибла ночью на 25 августа 1944 года в небе над Польшей вместе со штурманом Верой Белик, их самолёт сбил немецкий истребитель. Похоронена в братской могиле в польском городе Остроленка. Память о Татьяне Макаровой живет в Москве — её именем названа улица в Восточном административном округе.",
                years = "1920-1944 гг.",
                rewards = "Звание Героя Советского Союза.\nОрден Ленина.\nДва ордена Красного Знамени.\nОрден Отечественной войны 1-й степени.",
                imageName = "Tat_M_img",
                nextScene = "Tat_M_Scene",
                is_unlocked = true
            },
            
            new HeroData
            {
                id = 3,
                name = "Med",
                title = "Дмитрий Николаевич Медведев",
                description = "Дмитрий Николаевич Медведев — советский чекист и легендарный партизанский командир. Вступив в ряды ВЧК (Всероссийская чрезвычайная комиссия по борьбе с контрреволюцией и саботажем) в 1920 году, он прошёл долгий путь в органах госбезопасности, дважды подвергаясь увольнению в период репрессий. С началом Великой Отечественной войны его опыт был востребован: в 1941 году он возглавил первый диверсионный отряд «Митя», заброшенный в немецкий тыл, а с 1942 по 1944 год командовал знаменитым отрядом специального назначения «Победители» на Ровенщине и Львовщине. Под его руководством отряд провел сотни успешных операций, включая ликвидацию высокопоставленных оккупантов и спасение мирных жителей, а разведчик Николай Кузнецов добывал ценнейшие сведения. После войны он вышел в отставку в звании полковника и посвятил себя литературной деятельности. Его именем названы улицы в Брянске, Москве и других городах, а также выпускались почтовые марки с его портретом.",
                years = "1898-1954 гг.",
                rewards = "Медаль «Золотая Звезда» Героя Советского Союза № 4513.\nЧетыре ордена Ленина.\nОрден Красного Знамени.\nМедаль «Партизану Отечественной войны» I степени.\nМедаль «За победу над Германией в Великой Отечественной войне 1941—1945 гг.».",
                imageName = "Med_img",
                nextScene = "Med_Scene",
                is_unlocked = true
            },
            
            new HeroData
            {
                id = 4,
                name = "L_P",
                title = "Алексей Николаевич Павлов",
                description = "Алексей Николаевич Павлов — выдающийся советский лётчик-истребитель, гвардии капитан, Герой Советского Союза. Призванный в армию в 1940 году, он окончил легендарную Качинскую военную авиационную школу лётчиков и с мая 1943 года воевал в составе 156-го гвардейского истребительного авиационного полка 12-й гвардейской авиадивизии на 1-м Украинском фронте. За время войны Павлов проявил себя как умелый и отважный воздушный боец. К январю 1945 года он совершил 224 боевых вылета, провёл 40 воздушных боёв и лично сбил 16 вражеских самолётов, за что Указом Президиума Верховного Совета СССР от 10 апреля 1945 года был удостоен звания Героя Советского Союза с вручением медали «Золотая Звезда». К концу войны на его счету было уже около 300 боевых вылетов, 47 воздушных схваток и 17 лично сбитых самолётов противника. После войны Алексей Павлов продолжил службу в ВВС, в 1952 году с отличием окончил Военно-воздушную академию и служил на командных должностях. В запас уволился в 1976 году в звании полковника. Память о герое-лётчике увековечена в Москве — в районе Косино-Ухтомский, где он родился и вырос, его именем названа улица Лётчика Павлова.",
                years = "1922-1995 гг.",
                rewards = "Два ордена Красного Знамени.\nОрден Александра Невского.\nДва ордена Отечественной войны 1-ой степени\nОрден Отечественной войны 2-ой степени.\nТри ордена Красной Звезды.\nРяд медалей.",
                imageName = "L_P_img",
                nextScene = "L_P_Scene",
                is_unlocked = true
            },
            
            new HeroData
            {
                id = 5,
                name = "Dmitr",
                title = "Борис Николаевич Дмитриевский",
                description = "Борис Николаевич Дмитриевский — Герой Советского Союза. Родился в Москве. Окончив школу в 1941 году, с первых дней войны участвовал в противовоздушной обороне столицы, затем был призван в армию. В декабре 1942 года окончил Саратовское бронетанковое училище и весной 1943 года прибыл в 3-й гвардейский танковый корпус командиром танка Т-34. С мая 1943 года воевал на фронте, участвовал в освобождении Полтавы, Минска, Вильнюса. К февралю 1945 года, уже командуя танковой ротой, отличился в боях в Восточной Померании. Его рота прорвала немецкую оборону, уничтожила значительное количество техники и живой силы противника, захватила станцию с эшелонами. 10 марта Дмитриевский, прикрыв своим танком командира бригады, уничтожил вражескую батарею. 11 марта 1945 года в бою за Нойштадт (Польша) его танк был подбит, а сам он смертельно ранен. Похоронен в Лемборке. Посмертно удостоен звания Героя. В его честь в Москве названы улица и станция метро, в родной школе действует музей его имени.",
                years = "1922-1945 гг.",
                rewards = "Звание Героя Советского Союза - за «образцовое выполнение боевых заданий командования на фронте борьбы с немецкими захватчиками и проявленные при этом отвагу и геройство» (посмертно).\nОрден Красного Знамени.\nОрден Красной Звезды.\nОрден Отечественной войны 1-ой степени.",
                imageName = "Dmitr_img",
                nextScene = "Dmitr_Scene",
                is_unlocked = true
            }
        };
    }
    
    private void OnDestroy()
    {
        // Закрываем соединение с базой данных
        if (_reader != null && !_reader.IsClosed)
        {
            _reader.Close();
            _reader = null;
        }
        
        if (_command != null)
        {
            _command.Dispose();
            _command = null;
        }
        
        if (_connection != null)
        {
            if (_connection.State == ConnectionState.Open)
            {
                _connection.Close();
            }
            _connection.Dispose();
            _connection = null;
        }
        
        Debug.Log("Соединение с базой данных закрыто");
    }
}