# 3D Game 115 fall

## Requirements
- Unity 6000.3.24f1

## Contribution

### Formatter: 
Using [CSharpier - Code formatter](https://marketplace.visualstudio.com/items?itemName=csharpier.csharpier-vscode)

VScode settings: 
```json
"[csharp]": {
    "editor.defaultFormatter": "csharpier.csharpier-vscode",
    "editor.formatOnSave": true
},
```

Format entire project:
```
dotnet csharpier format .
```

If eccounter "contains more than one project file" error:
```
dotnet build 3D-Game-115-fall.slnx
```
