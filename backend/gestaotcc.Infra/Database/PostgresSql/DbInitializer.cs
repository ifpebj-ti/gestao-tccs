using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Entities.Campi;
using gestaotcc.Domain.Entities.CampiCourse;
using gestaotcc.Domain.Entities.Course;
using gestaotcc.Domain.Entities.DocumentType;
using gestaotcc.Domain.Entities.Profile;
using gestaotcc.Domain.Entities.User;
using gestaotcc.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace gestaotcc.Infra.Database.PostgresSql;

public static class DbInitializer
{
    public static void Initialize(AppDbContext context, IBcryptGateway bcryptGateway)
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

        // Seed Campi
        if (!context.Campi.Any())
        {
            context.Campi.AddRange(new CampiEntity[]
            {
                new() { Id = 1, Name = "IFPE - Abreu e Lima", City = "Abreu e Lima" },
                new() { Id = 2, Name = "IFPE - Afogados da Ingazeira", City = "Afogados da Ingazeira" },
                new() { Id = 3, Name = "IFPE - Barreiros", City = "Barreiros" },
                new() { Id = 4, Name = "IFPE - Caruaru", City = "Caruaru" },
                new() { Id = 5, Name = "IFPE - Garanhuns", City = "Garanhuns" },
                new() { Id = 6, Name = "IFPE - Ipojuca", City = "Ipojuca" },
                new() { Id = 7, Name = "IFPE - Jaboatão dos Guararapes", City = "Jaboatão dos Guararapes" },
                new() { Id = 8, Name = "IF Sertão-PE - Floresta", City = "Floresta" },
                new() { Id = 9, Name = "IF Sertão-PE - Salgueiro", City = "Salgueiro" },
                new() { Id = 10, Name = "IF Sertão-PE - Petrolina", City = "Petrolina" },
                new() { Id = 11, Name = "IFPE - Belo Jardim", City = "Belo Jardim" }
            });
            context.SaveChanges();

            // Sincroniza a sequence com o maior ID da tabela
            context.Database.ExecuteSqlRaw(@"
                SELECT setval(pg_get_serial_sequence('""Campi""', 'Id'), 
                (SELECT MAX(""Id"") FROM ""Campi""));
            ");
        }

        // Seed Courses
        if (!context.Courses.Any())
        {
            context.Courses.AddRange(new CourseEntity[]
            {
                new() { Id = 1, Name = "Técnico em Segurança do Trabalho", Level = "Técnico Integrado" },
                new() { Id = 2, Name = "Técnico em Enfermagem", Level = "Técnico Subsequente" },
                new() { Id = 3, Name = "Técnico em Informática", Level = "Técnico Integrado" },
                new() { Id = 4, Name = "Licenciatura em Computação", Level = "Superior - Licenciatura" },
                new() { Id = 5, Name = "Técnico em Alimentos", Level = "Técnico Integrado" },
                new() { Id = 6, Name = "Licenciatura em Química", Level = "Superior - Licenciatura" },
                new() { Id = 7, Name = "Técnico em Edificações", Level = "Técnico Subsequente" },
                new() { Id = 8, Name = "Engenharia Mecânica", Level = "Superior - Bacharelado" },
                new() { Id = 9, Name = "Técnico em Informática para Internet", Level = "Técnico Integrado" },
                new() { Id = 10, Name = "Licenciatura em Matemática", Level = "Superior - Licenciatura" },
                new() { Id = 11, Name = "Técnico em Química", Level = "Técnico Integrado" },
                new() { Id = 12, Name = "Engenharia Ambiental", Level = "Superior - Bacharelado" },
                new() { Id = 13, Name = "Técnico em Administração", Level = "Técnico Integrado" },
                new() { Id = 14, Name = "Análise e Desenvolvimento de Sistemas", Level = "Tecnólogo" },
                new() { Id = 15, Name = "Técnico em Agropecuária", Level = "Técnico Integrado" },
                new() { Id = 16, Name = "Gestão da Tecnologia da Informação", Level = "Tecnólogo" },
                new() { Id = 17, Name = "Técnico em Eletrotécnica", Level = "Técnico Integrado" },
                new() { Id = 18, Name = "Sistemas para Internet", Level = "Tecnólogo" },
                new() { Id = 19, Name = "Técnico em Agricultura", Level = "Técnico Integrado" },
                new() { Id = 20, Name = "Engenharia Agronômica", Level = "Superior - Bacharelado" },
                new() { Id = 21, Name = "Música", Level = "Superior - Licenciatura" },
                new() { Id = 22, Name = "Engenharia de Software", Level = "Superior - Bacharelado" }
            });
            context.SaveChanges();
            
            // Sincroniza a sequence com o maior ID da tabela
            context.Database.ExecuteSqlRaw(@"
                SELECT setval(pg_get_serial_sequence('""Courses""', 'Id'), 
                (SELECT MAX(""Id"") FROM ""Courses""));
            ");
        }

        // Seed Campi_Courses (ligação muitos-para-muitos)
        if (!context.CampiCourses.Any())
        {
            context.CampiCourses.AddRange(new CampiCourseEntity[]
            {
                new() { Id = 1, CampiId = 1, CourseId = 1 },
                new() { Id = 2, CampiId = 1, CourseId = 2 },
                new() { Id = 3, CampiId = 2, CourseId = 3 },
                new() { Id = 4, CampiId = 2, CourseId = 4 },
                new() { Id = 5, CampiId = 3, CourseId = 5 },
                new() { Id = 6, CampiId = 3, CourseId = 6 },
                new() { Id = 7, CampiId = 4, CourseId = 7 },
                new() { Id = 8, CampiId = 4, CourseId = 8 },
                new() { Id = 9, CampiId = 5, CourseId = 9 },
                new() { Id = 10, CampiId = 5, CourseId = 10 },
                new() { Id = 11, CampiId = 6, CourseId = 11 },
                new() { Id = 12, CampiId = 6, CourseId = 12 },
                new() { Id = 13, CampiId = 7, CourseId = 13 },
                new() { Id = 14, CampiId = 7, CourseId = 14 },
                new() { Id = 15, CampiId = 8, CourseId = 15 },
                new() { Id = 16, CampiId = 8, CourseId = 16 },
                new() { Id = 17, CampiId = 9, CourseId = 17 },
                new() { Id = 18, CampiId = 9, CourseId = 18 },
                new() { Id = 19, CampiId = 10, CourseId = 19 },
                new() { Id = 20, CampiId = 10, CourseId = 20 },

                new() { Id = 21, CampiId = 11, CourseId = 21 },
                new() { Id = 22, CampiId = 11, CourseId = 22 }
            });
            context.SaveChanges();
            
            // Sincroniza a sequence com o maior ID da tabela
            context.Database.ExecuteSqlRaw(@"
                SELECT setval(pg_get_serial_sequence('""CampiCourses""', 'Id'), 
                (SELECT MAX(""Id"") FROM ""CampiCourses""));
            ");
        }


        // Seed User
        if (!context.Users.Any())
        {
            var profile = context.Profiles.FirstOrDefault(x => x.Role == RoleType.ADMIN.ToString());
            var course = context.CampiCourses.FirstOrDefault(x => x.Id == 22);
            var password = bcryptGateway.GenerateHashPassword("Senha@123");

            context.Users.Add(new UserEntityBuilder()
                .WithName("dev-test")
                .WithCpf("000.000.000-00")
                .WithEmail("dev-test@ifpe.edu.br")
                .WithPassword(password)
                .WithCampiCourse(course)
                .WithProfile(new List<ProfileEntity>() { profile! })
                .Build());
            context.SaveChanges();
        }


        // Seed DocumentTypes
        if (!context.DocumentTypes.Any())
        {
            context.DocumentTypes.AddRange(new DocumentTypeEntity[]
            {
                new()
                {
                    Name = "ANEXO I - TERMO DE COMPROMISSO DE ORIENTAÇÃO", SignatureOrder = 2,
                    MethodSignature = MethoSignatureType.ONLY_DOCS.ToString()
                },
                new()
                {
                    Name = "ANEXO II - TERMO DE COMPROMISSO DE ORIENTAÇÃO VOLUNTÁRIA", SignatureOrder = 2,
                    MethodSignature = MethoSignatureType.NOT_ONLY_DOCS.ToString()
                },
                new()
                {
                    Name = "ANEXO VI - TERMO DE COMPROMISSO DO ORIENTANDO", SignatureOrder = 2,
                    MethodSignature = MethoSignatureType.NOT_ONLY_DOCS.ToString()
                },
                new()
                {
                    Name = "ANEXO III - CRONOGRAMA DE ENCONTROS", SignatureOrder = 3,
                    MethodSignature = MethoSignatureType.NOT_ONLY_DOCS.ToString()
                },
                new()
                {
                    Name = "ANEXO VII - FICHA DE ACOMPANHAMENTO", SignatureOrder = 3,
                    MethodSignature = MethoSignatureType.NOT_ONLY_DOCS.ToString()
                },
                new()
                {
                    Name = "ANEXO VIII - TERMO DE ENTREGA DO TCC PARA APRESENTAÇÃO PÚBLICA", SignatureOrder = 4,
                    MethodSignature = MethoSignatureType.NOT_ONLY_DOCS.ToString()
                },
                new()
                {
                    Name = "ANEXO IV - FICHA AVALIATIVA", SignatureOrder = 5,
                    MethodSignature = MethoSignatureType.NOT_ONLY_DOCS.ToString()
                }
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