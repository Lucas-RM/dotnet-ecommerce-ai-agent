using ECommerce.Application.DTOs;
using ECommerce.Application.Interfaces;
using ECommerce.Application.Services;
using ECommerce.Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ECommerce.Tests.Services;

public sealed class AuthServiceTests
{
    [Fact]
    public async Task Register_ShouldPersist_WhenEmailIsAvailable()
    {
        var userRepository = new Mock<IUserRepository>();
        userRepository.Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var refreshRepository = new Mock<IRefreshTokenRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var passwordHasher = new Mock<IPasswordHasher>();
        passwordHasher.Setup(x => x.Hash(It.IsAny<string>())).Returns("hash");
        var jwt = new Mock<IJwtTokenService>();
        var clock = new Mock<IDateTimeProvider>();
        clock.SetupGet(x => x.UtcNow).Returns(DateTime.UtcNow);

        var service = new AuthService(userRepository.Object, refreshRepository.Object, unitOfWork.Object, passwordHasher.Object, jwt.Object, clock.Object, NullLogger<AuthService>.Instance);
        var result = await service.RegisterAsync(new RegisterDto("Lucas", "lucas@mail.com", "Senha@123", "Senha@123"), CancellationToken.None);

        result.Email.Should().Be("lucas@mail.com");
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
