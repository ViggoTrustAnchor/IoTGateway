﻿using System;
using System.Threading.Tasks;
using Waher.Script.Abstraction.Elements;
using Waher.Script.Exceptions;
using Waher.Script.Objects;

namespace Waher.Script.Model
{
	/// <summary>
	/// Base class for unary boolean operators.
	/// </summary>
	public abstract class UnaryBooleanOperator : UnaryScalarOperator
	{
		/// <summary>
		/// Base class for binary boolean operators.
		/// </summary>
		/// <param name="Operand">Operand.</param>
		/// <param name="Start">Start position in script expression.</param>
		/// <param name="Length">Length of expression covered by node.</param>
		/// <param name="Expression">Expression containing script.</param>
		public UnaryBooleanOperator(ScriptNode Operand, int Start, int Length, Expression Expression)
			: base(Operand, Start, Length, Expression)
		{
		}

		/// <summary>
		/// Evaluates the node, using the variables provided in the <paramref name="Variables"/> collection.
		/// </summary>
		/// <param name="Variables">Variables collection.</param>
		/// <returns>Result.</returns>
		public override IElement Evaluate(Variables Variables)
		{
			IElement Op = this.op.Evaluate(Variables);

			if (Op.AssociatedObjectValue is bool BOp)
				return this.Evaluate(BOp);
			else
				return this.Evaluate(Op, Variables);
		}

		/// <summary>
		/// Evaluates the node, using the variables provided in the <paramref name="Variables"/> collection.
		/// </summary>
		/// <param name="Variables">Variables collection.</param>
		/// <returns>Result.</returns>
		public override async Task<IElement> EvaluateAsync(Variables Variables)
		{
			if (!this.isAsync)
				return base.Evaluate(Variables);

			IElement Op = await this.op.EvaluateAsync(Variables);

			if (Op.AssociatedObjectValue is bool BOp)
				return await this.EvaluateAsync(BOp);
			else
				return await this.EvaluateAsync(Op, Variables);
		}

		/// <summary>
		/// Evaluates the operator on scalar operands.
		/// </summary>
		/// <param name="Operand">Operand.</param>
		/// <param name="Variables">Variables collection.</param>
		/// <returns>Result</returns>
		public override IElement EvaluateScalar(IElement Operand, Variables Variables)
		{
			object Obj = Operand.AssociatedObjectValue;

			if (Obj is bool BOp)
				return this.Evaluate(BOp);
			else if (Expression.TryConvert(Obj, out bool b))
				return this.Evaluate(b);
			else
				throw new ScriptRuntimeException("Scalar operands must be boolean values.", this);
		}

		/// <summary>
		/// Evaluates the operator on scalar operands.
		/// </summary>
		/// <param name="Operand">Operand.</param>
		/// <param name="Variables">Variables collection.</param>
		/// <returns>Result</returns>
		public override async Task<IElement> EvaluateScalarAsync(IElement Operand, Variables Variables)
		{
			object Obj = Operand.AssociatedObjectValue;

			if (Obj is bool BOp)
				return await this.EvaluateAsync(BOp);
			else if (Expression.TryConvert(Obj, out bool b))
				return await this.EvaluateAsync(b);
			else
				throw new ScriptRuntimeException("Scalar operands must be boolean values.", this);
		}

		/// <summary>
		/// Evaluates the boolean operator.
		/// </summary>
		/// <param name="Operand">Operand.</param>
		/// <returns>Result</returns>
		public abstract IElement Evaluate(bool Operand);

		/// <summary>
		/// Evaluates the boolean operator.
		/// </summary>
		/// <param name="Operand">Operand.</param>
		/// <returns>Result</returns>
		public virtual Task<IElement> EvaluateAsync(bool Operand)
		{
			return Task.FromResult<IElement>(this.Evaluate(Operand));
		}

	}
}
