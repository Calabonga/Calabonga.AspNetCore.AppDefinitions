# Calabonga.AspNetCore.AppDefinitions

Сборка позволяет навести порядок в вашем `Program.cs`. Можно всё разложить "по полочкам". Чтобы воспользоваться сборкой надо:

## Установка nuget-пакета

Можно воспользоваться инструментов Visual Studio:

![image1](./docs/1.jpg)

Или можно просто прописать в файле проекта, но тогда надо будет подставить правильную версию пакета. Посмотреть последнюю актуальную версию можно на [nuget.org](https://www.nuget.org/packages/Calabonga.AspNetCore.AppDefinitions/).

![image2](./docs/2.jpg)

### Создание AppDefinitions

Создайте папку `Definitions` в вашем проекте. В папке создайте `ContainerDefinition` и унаследуйте его от `AppDefinition`, как показано ниже на картинке. После этого сделайте переопределение метода `ConfigureServices` и/или других методов и свойств.

![image3](./docs/3.jpg)

На этой картинке переопределено два метода:

![image4](./docs/4.jpg)

Подключите ваши определения как показано на этой картинке:

![image6](./docs/6.jpg)

Таких определений (наследников от `AppDefinition`) может быть сколько угодно (конечно же в разумных пределах). После старта приложения вы увидите (если включен уровень логирования `Debug`) список всех подключенных определений (`AppDefinition`). Например, в моём случае их 18.

![image5](./docs/5.jpg)

### Фильтрация и порядок

У каждого из созданных вами наследников от `AppDefinition` есть свойство `Enabled` и `OrderIndex`. Угадайте, что можно с ними (с `AppDefinition`ами) сделать?

# An English
Application Definitions base classes. The small but very helpful package that can help you to organize your ASP.NET Core application.

You can find more information in my blog [Nimble Framework](https://www.calabonga.net/blog/post/nimble-framework-v-6-1)
