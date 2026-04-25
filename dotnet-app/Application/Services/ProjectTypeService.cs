using FinanceTracker.Web.Application.Abstractions;
using FinanceTracker.Web.Domain.Entities;

namespace FinanceTracker.Web.Application.Services;

public class ProjectTypeService(
    IProjectTypeRepository projectTypeRepository,
    IProjectRepository projectRepository,
    IUnitOfWork unitOfWork)
{
    public Task<List<ProjectType>> GetAllAsync(CancellationToken cancellationToken = default)
        => projectTypeRepository.GetAllAsync(cancellationToken);

    public Task<ProjectType?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => projectTypeRepository.GetByIdAsync(id, cancellationToken);

    public async Task CreateAsync(string name, string? description, CancellationToken cancellationToken = default)
    {
        await projectTypeRepository.AddAsync(new ProjectType
        {
            Name = name.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim()
        }, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> UpdateAsync(int id, string name, string? description, CancellationToken cancellationToken = default)
    {
        var projectType = await projectTypeRepository.GetByIdAsync(id, cancellationToken);
        if (projectType is null)
        {
            return false;
        }

        projectType.Name = name.Trim();
        projectType.Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

        projectTypeRepository.Update(projectType);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var projectType = await projectTypeRepository.GetByIdAsync(id, cancellationToken);
        if (projectType is null)
        {
            return false;
        }

        if (await projectRepository.AnyByTypeAsync(id, cancellationToken))
        {
            throw new InvalidOperationException("This project type is already used by projects.");
        }

        projectTypeRepository.Remove(projectType);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}

