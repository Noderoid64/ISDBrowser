﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace HoustonBrowser.JS
{
    enum ESType
    {
        Undefined, Null, Boolean, String, Number, Object, Reference, List, Completion
    }

    [Flags]
    enum Attributes
    {
        ReadOnly=1, DontEnum=2, DontDelete=4, Internal=8
    }

    class ESInterpreter
    {
        private ESContext currentContext;

        internal ESContext CurrentContext { get => currentContext; set => currentContext = value; }

        internal Primitive Process(UnaryExpression rootExpr)
        {
            currentContext.ExpressionStack.Push(rootExpr);

            while (currentContext.ExpressionStack.Count != 0)
            {
                UnaryExpression node = currentContext.ExpressionStack.Pop();
                switch (node.Type)  
                {
                    case ExpressionType.ReturnExpression:
                        return EvalExpression(node.FirstValue);
                    default:
                        EvalExpression(node);
                        break;
                }
                
            }
            return new Primitive(ESType.Undefined, null);
        }

        internal Primitive EvalExpression(UnaryExpression expression)
        {
            switch (expression.Type)
            {
                case ExpressionType.Undefined:
                    return new Primitive(ESType.Undefined, null);
                case ExpressionType.Null:
                    return new Primitive(ESType.Null, null);
                case ExpressionType.Boolean:
                    SimpleExpression boolean = expression as SimpleExpression;
                    return new Primitive(ESType.Boolean, Convert.ToBoolean(boolean.Value));
                case ExpressionType.String:
                    SimpleExpression srt = expression as SimpleExpression;
                    return new Primitive(ESType.String, srt.Value);
                case ExpressionType.Number:
                    SimpleExpression num = expression as SimpleExpression;
                    double d;
                    double.TryParse((string)num.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out d);
                    return new Primitive(ESType.Number, d);

                case ExpressionType.Block:
                    ProcessBlock(expression);
                    break;

                case ExpressionType.VariableDeclaration:
                    ProcessVariableDeclaration(expression);
                    break;

                case ExpressionType.AssignmentExpression:
                    return ProcessAssignmentExpression(expression);

                case ExpressionType.IfExpression:
                    ProcessIfExpression(expression);
                    break;

                case ExpressionType.FunctionDeclaration:
                    ProcessFunctionDeclaration(expression);
                    break;
                case ExpressionType.Object: // TODO: add properties to this object 
                    HostObject p = new HostObject(null, "Object");
                    return p;

                case ExpressionType.Ident: // not by spec. should throw reference error
                    return ProcessIdent(expression);

                case ExpressionType.MemberExpression: // not by spec. see page 52
                    return ProcessMemberExpression(expression);

                case ExpressionType.BinaryExpression:
                    BinaryExpression binary = expression as BinaryExpression;
                    return ProcessBinaryExpression(EvalExpression(binary.FirstValue), EvalExpression(binary.SecondValue), binary.Oper);

                case ExpressionType.Arguments:
                    return ProcessArguments(expression);

                case ExpressionType.CallExpression:
                    return ProcessCallExpression(expression);

                case ExpressionType.NewExpression:
                    return ProcessNewExpression(expression);

                case ExpressionType.WhileExpression:
                    ProcessWhileExpression(expression);
                    break;
                default:
                    break;
            }
            return null;
        }

        private void ProcessWhileExpression(UnaryExpression expression)
        {
            BinaryExpression we = expression as BinaryExpression;
            while (TypeConverter.ToBoolean(EvalExpression(we.FirstValue)))
            {
                Process(we.SecondValue);
            }

        }

        private Primitive ProcessNewExpression(UnaryExpression expression)
        {
            BinaryExpression binaryExpression = expression as BinaryExpression;
            HostObject first = EvalExpression(binaryExpression.FirstValue) as HostObject;
            Primitive second = EvalExpression(binaryExpression.SecondValue);
            return first.Construct(null, second);
        }

        private Primitive ProcessCallExpression(UnaryExpression expression)
        {
            BinaryExpression callexpr = expression as BinaryExpression;
            HostObject memb = EvalExpression(callexpr.FirstValue) as HostObject;
            if (memb == null) throw new Exception($"Can't call function. There are no such object in context."); // should throw js error;
            UnaryExpression expr = callexpr.SecondValue;
            Primitive res = null;
            int i = 0;
            do
            {
                i++;
                Primitive args = null;
                HostObject go = currentContext.ExecContextStack.Peek();
                if (expr.Type == ExpressionType.CallExpression)
                {
                    args = EvalExpression(expr.FirstValue);
                    HostObject newExecContext = new HostObject(go, "Object");
                    currentContext.ExecContextStack.Push(newExecContext);
                    memb = memb.Call(currentContext.ExecContextStack.Peek(), args) as HostObject;
                    expr = (expr as BinaryExpression).SecondValue;
                }
                else
                {
                    args = EvalExpression(expr);
                    HostObject newExecContext = new HostObject(go, "Object");
                    newExecContext.Scope = memb;
                    currentContext.ExecContextStack.Push(newExecContext);
                    res = memb.Call(currentContext.ExecContextStack.Peek(), args);
                    break;
                }
            } while (expr!=null);
            for (; i > 0; i--) currentContext.ExecContextStack.Pop();
            return res;
        }

        private Primitive ProcessArguments(UnaryExpression expression)
        {
            Arguments primitive = expression as Arguments;
            List<Primitive> list;
            if (primitive.Args != null)
            {
                list = new List<Primitive>();
                foreach (var item in primitive.Args)
                {
                    list.Add(EvalExpression(item));
                }
                return new Primitive(ESType.List, list);
            }
            return new Primitive(ESType.List, null);
        }

        private Primitive ProcessMemberExpression(UnaryExpression expression)
        {
            BinaryExpression memexpr = expression as BinaryExpression;
            HostObject mObj = EvalExpression(memexpr.FirstValue) as HostObject;
            SimpleExpression mProp = memexpr.SecondValue as SimpleExpression;
            if (mProp == null) return mObj;
            return mObj.Get((string)mProp.Value).Value;
        }

        private Primitive ProcessIdent(UnaryExpression expression)
        {
            SimpleExpression ident = expression as SimpleExpression;
            HostObject scope = currentContext.ExecContextStack.Peek();
            while (scope!=null)
            {
                if (scope.HasProperty((string)ident.Value)) return scope.Get((string)ident.Value).Value;
                scope = scope.Scope;
            }
            return null;
        }

        private Primitive ProcessBinaryExpression(Primitive leftOp, Primitive rightOp,string oper)
        {
            Primitive res = null;
            double l = 0, r = 0;
            switch (oper)
            {
                            
                case ">":
                    l = TypeConverter.ToNumber(leftOp);
                    r = TypeConverter.ToNumber(rightOp);
                    res = new Primitive(ESType.Boolean, l > r);
                    break;
                case "+":

                    if (leftOp.Type == ESType.String || rightOp.Type == ESType.String)
                    {
                        string ls = TypeConverter.ToString(leftOp);
                        string rs = TypeConverter.ToString(rightOp);
                        return new Primitive(ESType.String, ls + rs);
                    }
                    l = TypeConverter.ToNumber(leftOp);
                    r = TypeConverter.ToNumber(rightOp);
                    res = new Primitive(ESType.Number, r + l);
                    break;
                case "-":
                    l = TypeConverter.ToNumber(leftOp);
                    r = TypeConverter.ToNumber(rightOp);
                    res = new Primitive(ESType.Number, l - r);
                    break;
                case "*":
                    l = TypeConverter.ToNumber(leftOp);
                    r = TypeConverter.ToNumber(rightOp);
                    res = new Primitive(ESType.Number, l * r);
                    break;
                case "/":
                    l = TypeConverter.ToNumber(leftOp);
                    r = TypeConverter.ToNumber(rightOp);
                    res = new Primitive(ESType.Number, l / r);
                    break;
                case "==":
                    if (leftOp.Type == rightOp.Type)
                    {
                        switch (leftOp.Type)
                        {
                            case ESType.Undefined:
                            case ESType.Null:
                                res = new Primitive(ESType.Boolean, true);
                                break;
                            case ESType.Number:
                                if (double.IsNaN((double)leftOp.Value) || double.IsNaN((double)rightOp.Value)) res = new Primitive(ESType.Boolean, false);
                                else if ((double)leftOp.Value == (double)rightOp.Value) res = new Primitive(ESType.Boolean, true);
                                else res = new Primitive(ESType.Boolean, false);
                                break;
                            case ESType.Boolean:
                                if ((bool)leftOp.Value == (bool)rightOp.Value) res = new Primitive(ESType.Boolean, true);
                                else res = new Primitive(ESType.Boolean, false);
                                break;
                            case ESType.String:
                                if ((string)leftOp.Value == (string)rightOp.Value) res = new Primitive(ESType.Boolean, true);
                                else res = new Primitive(ESType.Boolean, false);
                                break;
                            case ESType.Object:
                                if (leftOp.Value == rightOp.Value) res = new Primitive(ESType.Boolean, true);
                                else res = new Primitive(ESType.Boolean, false);
                                break;
                            default:
                                break;
                        }
                    }
                    else if (leftOp.Type == ESType.Null && rightOp.Type == ESType.Undefined) res = new Primitive(ESType.Boolean, true);
                    else if (rightOp.Type == ESType.Null && leftOp.Type == ESType.Undefined) res = new Primitive(ESType.Boolean, true);
                    else if (leftOp.Type == ESType.Number && rightOp.Type == ESType.String) res = ProcessBinaryExpression(leftOp, new Primitive(ESType.Number, TypeConverter.ToNumber(rightOp)), "==");
                    else if (rightOp.Type == ESType.String && leftOp.Type == ESType.Number) res = ProcessBinaryExpression(new Primitive(ESType.Number, TypeConverter.ToNumber(leftOp)), rightOp, "==");
                    else if (leftOp.Type == ESType.Boolean) res = ProcessBinaryExpression(new Primitive(ESType.Number, TypeConverter.ToNumber(leftOp)), rightOp, "==");
                    else if (rightOp.Type == ESType.Boolean ) res = ProcessBinaryExpression(leftOp, new Primitive(ESType.Number, TypeConverter.ToNumber(rightOp)), "==");
                    else res = new Primitive(ESType.Boolean, false);
                    break;
            }
            return res;
        }

        internal void ProcessFunctionDeclaration(UnaryExpression expression)
        {
            FunctionDeclaration func = expression as FunctionDeclaration;
            NativeObject funcObj = CreateFunction(expression);
            currentContext.ExecContextStack.Peek().Put(func.Id, funcObj);
            
        }

        private void ProcessIfExpression(UnaryExpression expression)
        {
            IfExpression ifExpr = expression as IfExpression;
            if (TypeConverter.ToBoolean(EvalExpression(ifExpr.Cond)))
            {
                currentContext.ExpressionStack.Push(ifExpr.FirstValue);
            }
            else
            {
                if (ifExpr.SecondValue != null) currentContext.ExpressionStack.Push(ifExpr.SecondValue);
            }
        }

        private Primitive ProcessAssignmentExpression(UnaryExpression expression)
        {
            BinaryExpression assignmentExpr = expression as BinaryExpression;
            Property a = EvalProperty(assignmentExpr.FirstValue);
            Primitive b = EvalExpression(assignmentExpr.SecondValue);
            switch (assignmentExpr.Oper)
            {
                case "=":
                    a.Value = b;
                    break;
                case "+=":
                    a.Value = ProcessBinaryExpression(a.Value,b,"+");
                    break;
                case "-=":
                    a.Value = ProcessBinaryExpression(a.Value,b,"+");
                    break;                
                default:
                    break;
            }
            
            return a.Value;
        }

        private void ProcessVariableDeclaration(UnaryExpression expression)
        {
            VariableDeclaration variable = expression as VariableDeclaration;
            foreach (var item in variable.Declarations)
            {
                if (item.FirstValue == null) currentContext.ExecContextStack.Peek().Put(item.Id, new Primitive(ESType.Undefined,null));
                else currentContext.ExecContextStack.Peek().Put(item.Id, EvalExpression(item.FirstValue));
            }
        }

        private void ProcessBlock(UnaryExpression expression)
        {
            Block block = expression as Block;
            for (int i = block.Body.Count - 1; i >= 0; i--)
            {
                currentContext.ExpressionStack.Push(block.Body[i]);
            }
        }

        private NativeObject CreateFunction(UnaryExpression expression)
        {
            FunctionDeclaration func = expression as FunctionDeclaration;
            HostObject go = currentContext.GlobalObject;
            NativeObject newFunc = new NativeObject((go.Get("Function").Value as HostObject).Prototype, "Function", func.FirstValue);
            NativeObject newObj = new NativeObject((go.Get("Object").Value as HostObject).Prototype, "Object");
            newObj.Put("constructor", newFunc, Attributes.DontEnum);
            newFunc.Scope = currentContext.ExecContextStack.Peek();
            newFunc.Put("length", new Primitive(ESType.Number, func.Parameters == null ? 0 : func.Parameters.Count));
            newFunc.Put("prototype", newObj, Attributes.DontDelete);
            return newFunc;
        }

        private Property EvalProperty(UnaryExpression expression)
        {
            switch (expression.Type)
            {
                case ExpressionType.Ident:
                    SimpleExpression ident = expression as SimpleExpression;
                    HostObject scope = currentContext.ExecContextStack.Peek();
                    while (scope != null)
                    {
                        if (scope.HasProperty((string)ident.Value)) return scope.Get((string)ident.Value);
                        scope = scope.Scope;
                    }
                    currentContext.ExecContextStack.Peek().Put((string)ident.Value, new Primitive(ESType.Undefined,null));
                    return currentContext.ExecContextStack.Peek().Get((string)ident.Value);
                case ExpressionType.MemberExpression:
                    BinaryExpression memexpr = expression as BinaryExpression;
                    HostObject mObj = EvalExpression(memexpr.FirstValue) as HostObject;
                    SimpleExpression mProp = memexpr.SecondValue as SimpleExpression;
                    //if (mProp == null) return mObj;
                    return mObj.Get((string)mProp.Value);
                default:
                    break;
            }
            return null;
        }
    }
}
