﻿using System;
using System.Threading.Tasks;
using Waher.Script.Abstraction.Elements;
using Waher.Script.Exceptions;
using Waher.Script.Objects;

namespace Waher.Script.Model
{
	/// <summary>
	/// Base class for binary double operators.
	/// </summary>
	public abstract class BinaryDoubleOperator : BinaryScalarOperator
	{
		/// <summary>
		/// Base class for binary double operators.
		/// </summary>
		/// <param name="Left">Left operand.</param>
		/// <param name="Right">Right operand.</param>
		/// <param name="Start">Start position in script expression.</param>
		/// <param name="Length">Length of expression covered by node.</param>
		/// <param name="Expression">Expression containing script.</param>
		public BinaryDoubleOperator(ScriptNode Left, ScriptNode Right, int Start, int Length, Expression Expression)
			: base(Left, Right, Start, Length, Expression)
		{
		}

		/// <summary>
		/// Evaluates the node, using the variables provided in the <paramref name="Variables"/> collection.
		/// </summary>
		/// <param name="Variables">Variables collection.</param>
		/// <returns>Result.</returns>
		public override IElement Evaluate(Variables Variables)
		{
			IElement L = this.left.Evaluate(Variables);
			IElement R = this.right.Evaluate(Variables);

			if (L.AssociatedObjectValue is double DL && R.AssociatedObjectValue is double DR)
				return this.Evaluate(DL, DR);
			else
				return this.Evaluate(L, R, Variables);
		}

		/// <summary>
		/// Evaluates the node, using the variables provided in the <paramref name="Variables"/> collection.
		/// </summary>
		/// <param name="Variables">Variables collection.</param>
		/// <returns>Result.</returns>
		public override async Task<IElement> EvaluateAsync(Variables Variables)
		{
			if (!this.isAsync)
				return this.Evaluate(Variables);

			IElement L = await this.left.EvaluateAsync(Variables);
			IElement R = await this.right.EvaluateAsync(Variables);

			if (L.AssociatedObjectValue is double DL && R.AssociatedObjectValue is double DR)
				return await this.EvaluateAsync(DL, DR);
			else
				return await this.EvaluateAsync(L, R, Variables);
		}

		/// <summary>
		/// Evaluates the operator on scalar operands.
		/// </summary>
		/// <param name="Left">Left value.</param>
		/// <param name="Right">Right value.</param>
		/// <param name="Variables">Variables collection.</param>
		/// <returns>Result</returns>
		public override IElement EvaluateScalar(IElement Left, IElement Right, Variables Variables)
		{
			object L = Left.AssociatedObjectValue;
			object R = Right.AssociatedObjectValue;

			if (!(L is double l) && !Expression.TryConvert(Left.AssociatedObjectValue, out l))
				throw new ScriptRuntimeException("Scalar operands must be double values.", this);

			if (!(R is double r) && !Expression.TryConvert(Right.AssociatedObjectValue, out r))
				throw new ScriptRuntimeException("Scalar operands must be double values.", this);

			return this.Evaluate(l, r);
		}

		/// <summary>
		/// Evaluates the operator on scalar operands.
		/// </summary>
		/// <param name="Left">Left value.</param>
		/// <param name="Right">Right value.</param>
		/// <param name="Variables">Variables collection.</param>
		/// <returns>Result</returns>
		public override Task<IElement> EvaluateScalarAsync(IElement Left, IElement Right, Variables Variables)
		{
			object L = Left.AssociatedObjectValue;
			object R = Right.AssociatedObjectValue;

			if (!(L is double l) && !Expression.TryConvert(Left.AssociatedObjectValue, out l))
				throw new ScriptRuntimeException("Scalar operands must be double values.", this);

			if (!(R is double r) && !Expression.TryConvert(Right.AssociatedObjectValue, out r))
				throw new ScriptRuntimeException("Scalar operands must be double values.", this);

			return this.EvaluateAsync(l, r);
		}

		/// <summary>
		/// Evaluates the double operator.
		/// </summary>
		/// <param name="Left">Left value.</param>
		/// <param name="Right">Right value.</param>
		/// <returns>Result</returns>
		public abstract IElement Evaluate(double Left, double Right);

		/// <summary>
		/// Evaluates the double operator.
		/// </summary>
		/// <param name="Left">Left value.</param>
		/// <param name="Right">Right value.</param>
		/// <returns>Result</returns>
		public virtual Task<IElement> EvaluateAsync(double Left, double Right)
		{
			return Task.FromResult<IElement>(this.Evaluate(Left, Right));
		}

	}
}
