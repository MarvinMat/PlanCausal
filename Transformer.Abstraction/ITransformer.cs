namespace Transformer.Abstraction;

public interface ITransformer <in TIn, out TOut>
{
    public TOut Transform(TIn input);
}