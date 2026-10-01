using Microsoft.AspNetCore.Mvc;
using System;
using System.ComponentModel;
using System.Reflection.Metadata;
using System.Xml.Linq;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<IRecipeRepository, RecipeRepository>();

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Recipe}/{action=Index}/{id?}");

app.Run();

public class Recipe
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<Ingredient> Ingredients { get; set; } = new();
}

public class Ingredient
{
    public string Name { get; set; } = string.Empty;
    public double Amount { get; set; }
    public string Unit { get; set; } = string.Empty;
}

public interface IRecipeRepository
{
    IEnumerable<Recipe> GetAll();
    Recipe? GetById(int id);
    void Save(Recipe recipe);
}

public class RecipeRepository : IRecipeRepository
{
    private readonly List<Recipe> _recipes = new()
    {
        new Recipe
        {
            Id = 1,
            Title = "Борщ",
            Description = "Традиційний український борщ",
            Ingredients = new List<Ingredient>
            {
                new Ingredient { Name = "Буряк", Amount = 2, Unit = "шт" },
                new Ingredient { Name = "Картопля", Amount = 4, Unit = "шт" }
            }
        }
    };

    public IEnumerable<Recipe> GetAll() => _recipes;

    public Recipe? GetById(int id) => _recipes.FirstOrDefault(r => r.Id == id);

    public void Save(Recipe recipe)
    {
        if (recipe.Id == 0)
        {
            recipe.Id = _recipes.Count > 0 ? _recipes.Max(r => r.Id) + 1 : 1;
            _recipes.Add(recipe);
        }
        else
        {
            var index = _recipes.FindIndex(r => r.Id == recipe.Id);
            if (index != -1)
            {
                _recipes[index] = recipe;
            }
        }
    }
}

public class RecipeController : Controller
{
    private readonly IRecipeRepository _repository;

    public RecipeController(IRecipeRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public IActionResult Index()
    {
        var recipes = _repository.GetAll();
        return Content(RenderPage("Список рецептів", RenderListHtml(recipes)), "text/html");
    }

    [HttpGet]
    public IActionResult Edit(int? id)
    {
        Recipe recipe = id.HasValue ? _repository.GetById(id.Value) ?? new Recipe() : new Recipe();
        return Content(RenderPage(id.HasValue ? "Редагувати рецепт" : "Створити рецепт", RenderFormHtml(recipe)), "text/html");
    }

    [HttpPost]
    public IActionResult Save([FromForm] Recipe recipe)
    {
        recipe.Ingredients.RemoveAll(i => string.IsNullOrWhiteSpace(i.Name));
        _repository.Save(recipe);
        return RedirectToAction("Index");
    }

    private string RenderPage(string title, string bodyContent)
    {
        return $@"
            <!DOCTYPE html>
            <html lang='uk'>
            <head>
                <meta charset='utf-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1'>
                <title>{title}</title>
                <link href='https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css' rel='stylesheet'>
            </head>
            <body class='bg-light'>
                <nav class='navbar navbar-expand-lg navbar-dark bg-primary mb-4'>
                    <div class='container'>
                        <a class='navbar-brand' href='/Recipe/Index'>Книга рецептів</a>
                    </div>
                </nav>
                <div class='container'>
                    {bodyContent}
                </div>
            </body>
            </html>";
    }

    private string RenderListHtml(IEnumerable<Recipe> recipes)
    {
        var html = "<div class='d-flex justify-content-between align-items-center mb-3'>" +
                   "<h2>Рецепти</h2>" +
                   "<a href='/Recipe/Edit' class='btn btn-success'>+ Створити рецепт</a>" +
                   "</div><div class='row'>";

        foreach (var r in recipes)
        {
            html += $@"
                <div class='col-md-6 mb-3'>
                    <div class='card shadow-sm'>
                        <div class='card-body'>
                            <h5 class='card-title'>{r.Title}</h5>
                            <p class='card-text'>{r.Description}</p>
                            <h6>Інгредієнти:</h6>
                            <ul>";
            foreach (var ing in r.Ingredients)
            {
                html += $"<li>{ing.Name} — {ing.Amount} {ing.Unit}</li>";
            }
            html += $@"
                            </ul>
                            <a href='/Recipe/Edit/{r.Id}' class='btn btn-primary btn-sm'>Редагувати</a>
                        </div>
                    </div>
                </div>";
        }

        html += "</div>";
        return html;
    }

    private string RenderFormHtml(Recipe recipe)
    {
        var ingRows = "";
        for (int i = 0; i < recipe.Ingredients.Count; i++)
        {
            ingRows += $@"
                <div class='row g-2 mb-2 ingredient-row'>
                    <div class='col-md-5'>
                        <input type='text' name='Ingredients[{i}].Name' value='{recipe.Ingredients[i].Name}' class='form-control' placeholder='Назва' required />
                    </div>
                    <div class='col-md-3'>
                        <input type='number' step='any' name='Ingredients[{i}].Amount' value='{recipe.Ingredients[i].Amount}' class='form-control' placeholder='Кількість' required />
                    </div>
                    <div class='col-md-3'>
                        <input type='text' name='Ingredients[{i}].Unit' value='{recipe.Ingredients[i].Unit}' class='form-control' placeholder='Од. виміру' required />
                    </div>
                    <div class='col-md-1'>
                        <button type='button' class='btn btn-danger w-100 remove-btn'>X</button>
                    </div>
                </div>";
        }

        return $@"
            <div class='card shadow-sm p-4'>
                <h2>{(recipe.Id == 0 ? "Створення рецепта" : "Редагування рецепта")}</h2>
                <form action='/Recipe/Save' method='post'>
                    <input type='hidden' name='Id' value='{recipe.Id}' />
                    <div class='mb-3'>
                        <label class='form-label'>Назва рецепта</label>
                        <input type='text' name='Title' value='{recipe.Title}' class='form-control' required />
                    </div>
                    <div class='mb-3'>
                        <label class='form-label'>Опис</label>
                        <textarea name='Description' class='form-control' rows='3' required>{recipe.Description}</textarea>
                    </div>

                    <h4>Інгредієнти</h4>
                    <div id='ingredientsContainer'>
                        {ingRows}
                    </div>

                    <button type='button' id='addIngredientBtn' class='btn btn-secondary my-3'>+ Додати інгредієнт (Add)</button>
                    <hr />
                    <button type='submit' class='btn btn-success'>Зберегти рецепт</button>
                    <a href='/Recipe/Index' class='btn btn-outline-secondary'>Скасувати</a>
                </form>
            </div>

            <script>
                document.addEventListener('DOMContentLoaded', function() {{
                    const container = document.getElementById('ingredientsContainer');
                    const addBtn = document.getElementById('addIngredientBtn');

                    function reindex() {{
                        const rows = container.querySelectorAll('.ingredient-row');
                        rows.forEach((row, index) => {{
                            row.querySelector('input[name*=\".Name\"]').setAttribute('name', `Ingredients[${{index}}].Name`);
                            row.querySelector('input[name*=\".Amount\"]').setAttribute('name', `Ingredients[${ { index} }].Amount`);
        row.querySelector('input[name*=\".Unit\"]').setAttribute('name', `Ingredients[${ { index} }].Unit`);
    }
});
                    }}

                    addBtn.addEventListener('click', function() {
    {
        const index = container.querySelectorAll('.ingredient-row').length;
        const row = document.createElement('div');
        row.className = 'row g-2 mb-2 ingredient-row';
        row.innerHTML = `
                            < div class= 'col-md-5' >
                                < input type = 'text' name = 'Ingredients[${{index}}].Name' class= 'form-control' placeholder = 'Назва' required />
                            </ div >
                            < div class= 'col-md-3' >
                                < input type = 'number' step = 'any' name = 'Ingredients[${{index}}].Amount' class= 'form-control' placeholder = 'Кількість' required />
                            </ div >
                            < div class= 'col-md-3' >
                                < input type = 'text' name = 'Ingredients[${{index}}].Unit' class= 'form-control' placeholder = 'Од. виміру' required />
                            </ div >
                            < div class= 'col-md-1' >
                                < button type = 'button' class= 'btn btn-danger w-100 remove-btn' > X </ button >
                            </ div >`;
container.appendChild(row);
                    }});

container.addEventListener('click', function(e) {
    {
        if (e.target && e.target.classList.contains('remove-btn'))
        {
            {
                e.target.closest('.ingredient-row').remove();
                reindex();
            }
        }
    }
});
                }});
            </ script > ";
    }
}
