using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Bulletin.Models;
using Construction.Domain.Entities;
using FluentValidation;
using MediatR;

namespace Construction.Application.Features.Bulletin.Commands.CreateBulletinPost;

/// <summary>Pins a new notice to the board.</summary>
public record CreateBulletinPostCommand : IRequest<BulletinPostDto>
{
    public string Title { get; init; } = null!;

    public string Body { get; init; } = null!;
}

public class CreateBulletinPostCommandValidator : AbstractValidator<CreateBulletinPostCommand>
{
    public CreateBulletinPostCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Body).NotEmpty().MaximumLength(4000);
    }
}

public class CreateBulletinPostCommandHandler
    : IRequestHandler<CreateBulletinPostCommand, BulletinPostDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CreateBulletinPostCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<BulletinPostDto> Handle(
        CreateBulletinPostCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedException("User is not authenticated.");

        var post = new BulletinPost
        {
            Title = request.Title.Trim(),
            Body = request.Body.Trim(),
            CreatedByUserId = userId,
        };

        _context.BulletinPosts.Add(post);
        await _context.SaveChangesAsync(cancellationToken);

        return new BulletinPostDto
        {
            Id = post.Id,
            Title = post.Title,
            Body = post.Body,
            CreatedByName = _currentUserService.Email ?? "",
            CreatedAt = post.CreatedAt,
            ViewCount = 0,
            Viewed = false,
        };
    }
}
