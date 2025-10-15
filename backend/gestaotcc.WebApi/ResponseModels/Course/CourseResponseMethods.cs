using gestaotcc.Domain.Entities.Course;

namespace gestaotcc.WebApi.ResponseModels.Course;

public class CourseResponseMethods
{
    public static CourseResponse CreateCourseResponse(CourseEntity course)
    {
        return new CourseResponseBuilder()
            .WithId(course.Id)
            .WithName(course.Name)
            .WithDescription(course.Level)
            .Build();
    }
}
