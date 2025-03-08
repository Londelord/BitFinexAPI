## BitFinex Sevice

### Запуск
1. Скачайте net9.0-windows.zip https://github.com/Londelord/BitFinexAPI/releases
2. Распакуйте архив
3. Запустите TestTask.GUI.exe

### Приложение
В данном приложении имеется 5 вкладок, навигация по которым осуществляется с помощью кнопок вверху окна. Имеющиеся вкладки:
1. Получение последних сделок по валютной паре. При запуске приложения автоматически выводится 10 сделок по валютной паре BTCUSD
2. Получение свечей. Можно изпользовать временной промежуток для получения свечей или количество свечей. Также имеется функция выбора длительности свечи
3. Получение сделок в прямом эфире. Укажите валютную пару и максимальное количество сделок, которые будут отображаться в окне. Затем нажмите кнопку "Подключиться" и подождите завершения подключения. После сделки начнут отображаться в окне.
4. Получение свечей в прямом эфире. Укажите валютную пару, длительность свечи и максимальное количество свечей, которые будут отображаться в окне. Затем нажмите кнопку "Подключиться" и подождите завершения подключения. После свечи начнут отображаться в окне.
5. Рассчет стоимости портфеля в валютах. Портфель уже сформирован (1 BTC, 15000 XRP, 50 XMR, 30 DASH) и изменить его нельзя. После нажатия кнопки "Рассчитать" выведется стоимость портфеля в каждой из данных валют.

### Connector
Класс connector находится в сборке TestTask.API. В нем реализованы методы получения данных с помощью REST API (получение сделок, свечей и последних цен валютных пар с помощью тикеров) и WebSocket (получение сделок и свечей). Также имеются события, на которые можно подписаться. Класс поддерживает несколько подключений и асинхронное оповещение каждого подписчика благодаря отдельному сервису WebSocketSubscription. 
