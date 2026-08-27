namespace OpenBullet.Views.StackerBlocks;

public class KeyFullId
{
	public int KeyId { get; set; }

	public int ParentId { get; set; }

	public bool LeftTermInitialized { get; set; }

	public bool ConditionInitialized { get; set; }

	public KeyFullId()
	{
		KeyId = 0;
		ParentId = 0;
		LeftTermInitialized = false;
		ConditionInitialized = false;
	}
}
