using gestaotcc.Domain.Entities.Semester;

namespace gestaotcc.Application.Gateways;

public interface ISemesterGateway
{
    Task<List<SemesterEntity>> FindAll();
    Task<List<SemesterEntity>> FindActive();
    Task<SemesterEntity?> FindById(long id);
    Task<SemesterEntity> Insert(SemesterEntity semester);
    Task<SemesterEntity> Update(SemesterEntity semester);
    Task Delete(long id);
}
