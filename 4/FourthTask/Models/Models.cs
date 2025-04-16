namespace FourthTask.Models;

public record BaseModel(string Value);

public record Document(string Value) : BaseModel(Value);

public record Token(string Value) : BaseModel(Value);
