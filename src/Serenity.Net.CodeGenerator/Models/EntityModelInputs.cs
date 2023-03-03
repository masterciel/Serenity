﻿namespace Serenity.CodeGenerator;

public class EntityModelInputs : IEntityModelInputs
{
    public GeneratorConfig Config { get; set; }
    public string ConnectionKey { get; set; }
    public IEntityDataSchema DataSchema { get; set; }
    public string Identifier { get; set; }
    public string Module { get; set; }
    public bool Net5Plus { get; set; } = true;
    public bool OmitSchemaInExpressions { get; set; }
    public string PermissionKey { get; set; }
    public string Schema { get; set; }
    public string Table { get; set; }
}