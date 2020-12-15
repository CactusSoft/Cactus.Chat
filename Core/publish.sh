#!/bin/bash
nuget push Cactus.Chat/bin/Release/Cactus.Chat."$1".nupkg -Source https://api.nuget.org/v3/index.json
nuget push Cactus.Chat.Autofac/bin/Release/Cactus.Chat.Autofac."$1".nupkg -Source https://api.nuget.org/v3/index.json
nuget push Cactus.Chat.Connection/bin/Release/Cactus.Chat.Connection."$1".nupkg -Source https://api.nuget.org/v3/index.json
nuget push Cactus.Chat.CoreSignalr/bin/Release/Cactus.Chat.CoreSignalr."$1".nupkg -Source https://api.nuget.org/v3/index.json
nuget push Cactus.Chat.Events/bin/Release/Cactus.Chat.Events."$1".nupkg -Source https://api.nuget.org/v3/index.json
nuget push Cactus.Chat.InMemory/bin/Release/Cactus.Chat.InMemory."$1".nupkg -Source https://api.nuget.org/v3/index.json
nuget push Cactus.Chat.Model/bin/Release/Cactus.Chat.Model."$1".nupkg -Source https://api.nuget.org/v3/index.json
nuget push Cactus.Chat.Mongo/bin/Release/Cactus.Chat.Mongo."$1".nupkg -Source https://api.nuget.org/v3/index.json
nuget push Cactus.Chat.Transport/bin/Release/Cactus.Chat.Transport."$1".nupkg -Source https://api.nuget.org/v3/index.json
nuget push Cactus.Chat.WebSockets/bin/Release/Cactus.Chat.WebSockets."$1".nupkg -Source https://api.nuget.org/v3/index.json
