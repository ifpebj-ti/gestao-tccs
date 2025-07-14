using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Entities.Course;
using gestaotcc.Domain.Entities.DocumentType;
using gestaotcc.Domain.Entities.Profile;
using gestaotcc.Domain.Entities.User;
using gestaotcc.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace gestaotcc.Infra.Database.PostgresSql;

public static class DbInitializer
{
    public static void Initialize(AppDbContext context)
    {
        // Seed Profiles
        if (!context.Profiles.Any())
        {
            context.Profiles.AddRange(new ProfileEntity[]
            {
                new() { Role = "ADMIN" },
                new() { Role = "COORDINATOR" },
                new() { Role = "SUPERVISOR" },
                new() { Role = "ADVISOR" },
                new() { Role = "BANKING" },
                new() { Role = "STUDENT" },
                new() { Role = "LIBRARY" }
            });
            context.SaveChanges();
        }
        
        // Seed User
        if (!context.Users.Any())
        {
            var profile = context.Profiles.FirstOrDefault(x => x.Role == RoleType.ADMIN.ToString());
            
            context.Users.Add(new UserEntityBuilder()
                .WithName("dev-test")
                .WithCpf("000.000.000-00")
                .WithEmail("dev-test@gmail.com")
                .WithPassword("Senha@123")
                .WithProfile(new List<ProfileEntity>() { profile! })
                .Build());
            context.SaveChanges();
        }

        // Seed Courses
        if (!context.Courses.Any())
        {
            context.Courses.AddRange(new CourseEntity[]
            {
                new() { Name = CourseType.ENGENHARIA_DE_SOFTWARE.ToString(), Description = "Um curso para concertar geladeira" },
                new() { Name = CourseType.MUSIC.ToString(), Description = "Um curso para ouvir spotify" }
            });
            context.SaveChanges();
        }

        // Seed DocumentTypes
        if (!context.DocumentTypes.Any())
        {
            context.DocumentTypes.AddRange(new DocumentTypeEntity[]
            {
                new() { Name = "ANEXO I - TERMO DE COMPROMISSO DE ORIENTAÇÃO", SignatureOrder = 2, MethodSignature = MethoSignatureType.ONLY_DOCS.ToString() },
                new() { Name = "ANEXO II - TERMO DE COMPROMISSO DE ORIENTAÇÀO VOLUNTÁRIA", SignatureOrder = 2, MethodSignature = MethoSignatureType.NOT_ONLY_DOCS.ToString() },
                new() { Name = "ANEXO VI - TERMO DE COMPROMISSO DO ORIENTANDO", SignatureOrder = 2, MethodSignature = MethoSignatureType.NOT_ONLY_DOCS.ToString() },
                new() { Name = "ANEXO III - CRONOGRAMA DE ENCONTROS", SignatureOrder = 3, MethodSignature = MethoSignatureType.NOT_ONLY_DOCS.ToString() },
                new() { Name = "ANEXO VII - FICHA DE ACOMPANHAMENTO", SignatureOrder = 3, MethodSignature = MethoSignatureType.NOT_ONLY_DOCS.ToString() },
                new() { Name = "ANEXO VIII - TERMO DE ENTREGA DO TCC PARA APRESENTAÇÃO PÚBLICA", SignatureOrder = 4, MethodSignature = MethoSignatureType.NOT_ONLY_DOCS.ToString() },
                new() { Name = "ANEXO IV - FICHA AVALIATIVA", SignatureOrder = 5, MethodSignature = MethoSignatureType.NOT_ONLY_DOCS.ToString() }
            });
            context.SaveChanges();
        }
        
        SeedDocumentTypeProfile(context);
    }
    
    private static void SeedDocumentTypeProfile(AppDbContext context)
    {
        var existing = context.Database.ExecuteSqlRaw(@"
            DO $$
            BEGIN
                IF NOT EXISTS (
                    SELECT 1 FROM ""documentType_profile""
                ) THEN
                    INSERT INTO ""documentType_profile"" (""DocumentTypesId"", ""ProfilesId"") VALUES
                        (1, 4),
                        (2, 4),
                        (3, 4),
                        (1, 6),
                        (2, 6),
                        (3, 6),
                        (4, 4),
                        (5, 4),
                        (4, 6),
                        (5, 6),
                        (6, 4),
                        (7, 4),
                        (7, 5),
                        (7, 6);
                END IF;
            END
            $$;
        ");
    }
}