using RuriLib.ViewModels;

namespace RuriLib.Models;

public abstract class Persistable<T> : ViewModelBase
{
	public T Id { get; set; }
}
