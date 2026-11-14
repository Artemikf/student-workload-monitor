namespace StudentWorkload.Domain.Entities;

public class Subject
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty; // Название предмета (напр. "Мультиплатформне програмування")
    public string TeacherName { get; set; } = string.Empty;
    public int TotalCredits { get; set; } // Кредиты ECTS или общая сложность

    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;

    public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
}