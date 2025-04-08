namespace RayWork.Commands;

[AttributeUsage(AttributeTargets.Method)]
public class CommandAttribute(string name) : Attribute
{
    public readonly string Name = name;
}

[AttributeUsage(AttributeTargets.Method)]
public class HelpAttribute(string help) : Attribute
{
    public readonly string Help = help;
}