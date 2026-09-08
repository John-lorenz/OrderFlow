using FluentValidation;
using MediatR;
using OrderFlow.Application.Customers.Dtos;
using OrderFlow.Domain.Entities;
using OrderFlow.Domain.Exceptions;
using OrderFlow.Domain.Repositories;
using OrderFlow.Domain.ValueObjects;

namespace OrderFlow.Application.Customers.Commands;

public sealed record CreateCustomerCommand(
    string Name,
    string Email,
    string Document,
    string? Phone,
    string Street,
    string City,
    string State,
    string PostalCode,
    string Country) : IRequest<CustomerDto>;

public sealed class CreateCustomerCommandValidator : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Document).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Street).NotEmpty().MaximumLength(200);
        RuleFor(x => x.City).NotEmpty().MaximumLength(100);
    }
}

public sealed class CreateCustomerCommandHandler : IRequestHandler<CreateCustomerCommand, CustomerDto>
{
    private readonly ICustomerRepository _customers;
    private readonly IUnitOfWork _unitOfWork;

    public CreateCustomerCommandHandler(ICustomerRepository customers, IUnitOfWork unitOfWork)
    {
        _customers = customers;
        _unitOfWork = unitOfWork;
    }

    public async Task<CustomerDto> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
    {
        var existing = await _customers.GetByDocumentAsync(request.Document, cancellationToken);
        if (existing is not null)
        {
            throw new ConflictException("A customer with this document already exists.");
        }

        var customer = Customer.Create(
            request.Name,
            request.Email,
            request.Document,
            request.Phone,
            Address.Create(request.Street, request.City, request.State, request.PostalCode, request.Country));

        await _customers.AddAsync(customer, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return CustomerDto.From(customer);
    }
}

public sealed record UpdateCustomerCommand(
    Guid Id,
    string Name,
    string Email,
    string Document,
    string? Phone,
    string Street,
    string City,
    string State,
    string PostalCode,
    string Country) : IRequest<CustomerDto>;

public sealed class UpdateCustomerCommandHandler : IRequestHandler<UpdateCustomerCommand, CustomerDto>
{
    private readonly ICustomerRepository _customers;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateCustomerCommandHandler(ICustomerRepository customers, IUnitOfWork unitOfWork)
    {
        _customers = customers;
        _unitOfWork = unitOfWork;
    }

    public async Task<CustomerDto> Handle(UpdateCustomerCommand request, CancellationToken cancellationToken)
    {
        var customer = await _customers.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new EntityNotFoundException("Customer", request.Id);

        customer.Update(
            request.Name,
            request.Email,
            request.Document,
            request.Phone,
            Address.Create(request.Street, request.City, request.State, request.PostalCode, request.Country));

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return CustomerDto.From(customer);
    }
}

public sealed record DeactivateCustomerCommand(Guid Id) : IRequest;

public sealed class DeactivateCustomerCommandHandler : IRequestHandler<DeactivateCustomerCommand>
{
    private readonly ICustomerRepository _customers;
    private readonly IUnitOfWork _unitOfWork;

    public DeactivateCustomerCommandHandler(ICustomerRepository customers, IUnitOfWork unitOfWork)
    {
        _customers = customers;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeactivateCustomerCommand request, CancellationToken cancellationToken)
    {
        var customer = await _customers.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new EntityNotFoundException("Customer", request.Id);

        customer.Deactivate();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
