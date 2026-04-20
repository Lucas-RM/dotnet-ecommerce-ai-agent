namespace ECommerce.Application.Interfaces;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);

    void ClearTracking();

    /// <summary>
    /// Executa a operação dentro de uma transação com isolamento <see cref="System.Data.IsolationLevel.Serializable"/>,
    /// evitando condições de corrida entre mutações do mesmo carrinho (ex.: checkout vs. adicionar item).
    /// </summary>
    Task ExecuteInSerializableTransactionAsync(Func<Task> action, CancellationToken cancellationToken);

    Task<TResult> ExecuteInSerializableTransactionAsync<TResult>(Func<Task<TResult>> action, CancellationToken cancellationToken);
}
