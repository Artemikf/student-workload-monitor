namespace StudentWorkload.Domain.Entities;

public class Assignment
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty; // Название лабораторной/модуля
    public string Description { get; set; } = string.Empty;
    public DateTime DueDate { get; set; } // Дедлайн
    public double EstimatedHours { get; set; } // Оценочная трудоемкость в часах (сколько нужно времени)
    public bool IsCompleted { get; set; } = false; // Статус выполнения

    public int SubjectId { get; set; }
    public Subject Subject { get; set; } = null!;
}