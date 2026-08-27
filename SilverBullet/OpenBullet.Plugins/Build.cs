using System;
using System.Reflection;
using OpenBullet.Views.UserControls;
using PluginFramework.Attributes;
using RuriLib.ViewModels;

namespace OpenBullet.Plugins;

public static class Build
{
	public static UserControlContainer InputField(object plugin, PropertyInfo property, ViewModelBase viewModel = null)
	{
		InputField customAttribute = ((MemberInfo)property).GetCustomAttribute<InputField>();
		dynamic val = Convert.ChangeType(property.GetValue(plugin), property.PropertyType);
		IControl userControl;
		if (!(customAttribute is InfoText))
		{
			Text val2 = (Text)(object)((customAttribute is Text) ? customAttribute : null);
			if (val2 == null)
			{
				Numeric val3 = (Numeric)(object)((customAttribute is Numeric) ? customAttribute : null);
				if (val3 == null)
				{
					if (!(customAttribute is Checkbox))
					{
						TextMulti val4 = (TextMulti)(object)((customAttribute is TextMulti) ? customAttribute : null);
						if (val4 == null)
						{
							FilePicker val5 = (FilePicker)(object)((customAttribute is FilePicker) ? customAttribute : null);
							if (val5 == null)
							{
								Dropdown val6 = (Dropdown)(object)((customAttribute is Dropdown) ? customAttribute : null);
								if (val6 == null)
								{
									if (!(customAttribute is WordlistPicker))
									{
										if (!(customAttribute is ConfigPicker))
										{
											throw new NotImplementedException();
										}
										userControl = new UserControlConfig();
									}
									else
									{
										userControl = new UserControlWordlist();
									}
								}
								else
								{
									userControl = new UserControlDropdown(val, val6.options, viewModel);
								}
							}
							else
							{
								userControl = new UserControlFilePicker(val, val5.filter);
							}
						}
						else
						{
							userControl = new UserControlTextMulti(val, val4.readOnly, viewModel);
						}
					}
					else
					{
						userControl = new UserControlCheckbox(val, viewModel);
					}
				}
				else
				{
					userControl = new UserControlNumeric(val, val3.minimum, val3.maximum, viewModel);
				}
			}
			else
			{
				userControl = new UserControlText(val, val2.readOnly, viewModel);
			}
		}
		else
		{
			userControl = new UserControlInfoText(val, viewModel);
		}
		return new UserControlContainer(property.Name, userControl, customAttribute.label, customAttribute.tooltip);
	}

	public static ButtonContainer Button(MethodInfo method, PluginControl pluginControl)
	{
		return new ButtonContainer(((MemberInfo)method).GetCustomAttribute<Button>().text, method.Name, pluginControl);
	}
}
