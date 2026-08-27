using System;
using System.Windows;
using Newtonsoft.Json;
using RuriLib.ViewModels;

namespace OpenBullet.Models;

public class Source : ViewModelBase
{
	public enum AuthMode
	{
		ApiKey,
		UserPass
	}

	private int _id;

	private string apiUrl = "";

	private AuthMode auth;

	private string apiKey = "";

	private string username = "";

	private string password = "";

	private bool _authInitialized;

	public int Id
	{
		get
		{
			return _id;
		}
		set
		{
			if (_id != value)
			{
				_id = value;
				OnPropertyChanged("Id");
			}
		}
	}

	public string ApiUrl
	{
		get
		{
			return apiUrl;
		}
		set
		{
			if (!string.Equals(apiUrl, value, StringComparison.Ordinal))
			{
				apiUrl = value;
				OnPropertyChanged("ApiUrl");
			}
		}
	}

	public AuthMode Auth
	{
		get
		{
			return auth;
		}
		set
		{
			if (auth != value)
			{
				auth = value;
				OnPropertyChanged("Auth");
				OnPropertyChanged("ApiKeyVisible");
				OnPropertyChanged("UserPassVisible");
			}
		}
	}

	public string ApiKey
	{
		get
		{
			return apiKey;
		}
		set
		{
			if (!string.Equals(apiKey, value, StringComparison.Ordinal))
			{
				apiKey = value;
				OnPropertyChanged("ApiKey");
			}
		}
	}

	public string Username
	{
		get
		{
			return username;
		}
		set
		{
			if (!string.Equals(username, value, StringComparison.Ordinal))
			{
				username = value;
				OnPropertyChanged("Username");
			}
		}
	}

	public string Password
	{
		get
		{
			return password;
		}
		set
		{
			if (!string.Equals(password, value, StringComparison.Ordinal))
			{
				password = value;
				OnPropertyChanged("Password");
			}
		}
	}

	[JsonIgnore]
	public bool AuthInitialized
	{
		get
		{
			return _authInitialized;
		}
		set
		{
			if (_authInitialized != value)
			{
				_authInitialized = value;
				OnPropertyChanged("AuthInitialized");
			}
		}
	}

	[JsonIgnore]
	public Visibility ApiKeyVisible
	{
		get
		{
			if (Auth != 0)
			{
				return Visibility.Collapsed;
			}
			return Visibility.Visible;
		}
	}

	[JsonIgnore]
	public Visibility UserPassVisible
	{
		get
		{
			if (Auth != AuthMode.UserPass)
			{
				return Visibility.Collapsed;
			}
			return Visibility.Visible;
		}
	}

	public Source(int id)
	{
		Id = id;
	}
}
