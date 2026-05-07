using NSubstitute;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using UpdateContext = TelegramBotFlow.Core.Context.UpdateContext;

namespace TelegramBotFlow.Core.Tests.ContextModel;

public sealed class UpdateContextFactsTests
{
    [Fact]
    public void MessageUpdate_ExtractsCoreFields()
    {
        var update = new Update
        {
            Message = new Message
            {
                Id = 77,
                Text = "/start referral",
                Chat = new Chat { Id = 1234, Type = ChatType.Private },
                From = new User { Id = 5678, FirstName = "Tester" },
                Date = DateTime.UtcNow
            }
        };

        IServiceProvider services = Substitute.For<IServiceProvider>();

        var context = new UpdateContext(update, services);

        Assert.Equal(UpdateType.Message, context.UpdateType);
        Assert.Equal(1234, context.ChatId);
        Assert.Equal(5678, context.UserId);
        Assert.Equal(77, context.MessageId);
        Assert.Equal("/start referral", context.MessageText);
        Assert.Equal("referral", context.CommandArgument);
        Assert.Null(context.CallbackData);
        Assert.False(context.IsAdmin);
    }

    [Fact]
    public void CallbackUpdate_ExtractsCoreFields()
    {
        var update = new Update
        {
            CallbackQuery = new CallbackQuery
            {
                Id = "cb-1",
                Data = "nav:settings",
                From = new User { Id = 51, FirstName = "Tester" },
                Message = new Message
                {
                    Id = 909,
                    Chat = new Chat { Id = 808, Type = ChatType.Private },
                    Date = DateTime.UtcNow
                }
            }
        };

        var context = new UpdateContext(update, Substitute.For<IServiceProvider>());

        Assert.Equal(UpdateType.CallbackQuery, context.UpdateType);
        Assert.Equal(808, context.ChatId);
        Assert.Equal(51, context.UserId);
        Assert.Equal(909, context.MessageId);
        Assert.Equal("nav:settings", context.CallbackData);
        Assert.Null(context.MessageText);
        Assert.Null(context.CommandArgument);
    }

    [Fact]
    public void PhotoMessage_ExtractsPhotosAndHasMedia()
    {
        var photos = new[]
        {
            new PhotoSize { FileId = "photo1", FileUniqueId = "u1", Width = 100, Height = 100 },
            new PhotoSize { FileId = "photo2", FileUniqueId = "u2", Width = 200, Height = 200 }
        };

        var update = new Update
        {
            Message = new Message
            {
                Id = 1,
                Photo = photos,
                Chat = new Chat { Id = 10, Type = ChatType.Private },
                From = new User { Id = 20, FirstName = "Tester" },
                Date = DateTime.UtcNow
            }
        };

        var context = new UpdateContext(update, Substitute.For<IServiceProvider>());

        Assert.Same(photos, context.Photos);
        Assert.True(context.HasMedia);
    }

    [Fact]
    public void DocumentMessage_ExtractsDocumentAndHasMedia()
    {
        var document = new Document { FileId = "doc1", FileUniqueId = "ud1" };

        var update = new Update
        {
            Message = new Message
            {
                Id = 1,
                Document = document,
                Chat = new Chat { Id = 10, Type = ChatType.Private },
                From = new User { Id = 20, FirstName = "Tester" },
                Date = DateTime.UtcNow
            }
        };

        var context = new UpdateContext(update, Substitute.For<IServiceProvider>());

        Assert.Same(document, context.Document);
        Assert.True(context.HasMedia);
    }

    [Fact]
    public void ContactMessage_ExtractsContactAndHasMediaIsFalse()
    {
        var contact = new Contact { PhoneNumber = "+123", FirstName = "John" };

        var update = new Update
        {
            Message = new Message
            {
                Id = 1,
                Contact = contact,
                Chat = new Chat { Id = 10, Type = ChatType.Private },
                From = new User { Id = 20, FirstName = "Tester" },
                Date = DateTime.UtcNow
            }
        };

        var context = new UpdateContext(update, Substitute.For<IServiceProvider>());

        Assert.Same(contact, context.Contact);
        Assert.False(context.HasMedia);
    }

    [Fact]
    public void LocationMessage_ExtractsLocation()
    {
        var location = new Location { Latitude = 55.75f, Longitude = 37.62f };

        var update = new Update
        {
            Message = new Message
            {
                Id = 1,
                Location = location,
                Chat = new Chat { Id = 10, Type = ChatType.Private },
                From = new User { Id = 20, FirstName = "Tester" },
                Date = DateTime.UtcNow
            }
        };

        var context = new UpdateContext(update, Substitute.For<IServiceProvider>());

        Assert.Same(location, context.Location);
        Assert.False(context.HasMedia);
    }

    [Fact]
    public void TextMessage_AllMediaPropertiesNull_HasMediaFalse()
    {
        var update = new Update
        {
            Message = new Message
            {
                Id = 1,
                Text = "hello",
                Chat = new Chat { Id = 10, Type = ChatType.Private },
                From = new User { Id = 20, FirstName = "Tester" },
                Date = DateTime.UtcNow
            }
        };

        var context = new UpdateContext(update, Substitute.For<IServiceProvider>());

        Assert.Null(context.Photos);
        Assert.Null(context.Document);
        Assert.Null(context.Contact);
        Assert.Null(context.Location);
        Assert.Null(context.Voice);
        Assert.Null(context.Video);
        Assert.Null(context.VideoNote);
        Assert.False(context.HasMedia);
    }
}