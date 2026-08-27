using System.Collections.ObjectModel;
using System.Linq;
using RuriLib.ViewModels;

namespace OpenBullet.ViewModels;

public class ConfigOtherOptionsInputViewModel : ViewModelBase
{
	private ObservableCollection<CustomInput> inputsList = new ObservableCollection<CustomInput>();

	public ObservableCollection<CustomInput> InputsList
	{
		get
		{
			return inputsList;
		}
		set
		{
			if (!object.Equals(inputsList, value))
			{
				inputsList = value;
				OnPropertyChanged("InputsList");
			}
		}
	}

	public CustomInput GetInputById(int id)
	{
		return InputsList.Where((CustomInput x) => x.Id == id).First();
	}

	public void RemoveInputById(int id)
	{
		InputsList.Remove(GetInputById(id));
	}
}
