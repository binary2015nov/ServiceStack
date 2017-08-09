using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Templates;
using ServiceStack.Text;
using ServiceStack.VirtualPath;

namespace ServiceStack.WebHost.Endpoints.Tests.TemplateTests
{
    public class TemplateDefaultFiltersTests
    {
        public TemplateContext CreateContext(Dictionary<string, object> args = null)
        {
            var context = new TemplateContext
            {
                Args =
                {
                    ["foo"] = "bar",
                    ["intVal"] = 1,
                    ["doubleVal"] = 2.2
                }
            }.Init();
            
            args.Each((key,val) => context.Args[key] = val);
            
            return context;
        }

        [Test]
        public async Task Does_default_filter_raw()
        {
            var context = CreateContext();
            context.VirtualFiles.WriteFile("page.html", "<h1>{{ '<script>' }}</h1>");
            context.VirtualFiles.WriteFile("page-raw.html", "<h1>{{ '<script>' | raw }}</h1>");

            var result = await new PageResult(context.GetPage("page")).RenderToStringAsync();
            Assert.That(result, Is.EqualTo("<h1>&lt;script&gt;</h1>"));

            result = await new PageResult(context.GetPage("page-raw")).RenderToStringAsync();
            Assert.That(result, Is.EqualTo("<h1><script></h1>"));
        }

        [Test]
        public async Task Does_default_filter_json()
        {
            var context = CreateContext();
            context.VirtualFiles.WriteFile("page.html", "var model = {{ model | json }};");

            var result = await new PageResult(context.GetPage("page"))
            {
                Model = new Model
                {
                    Id = 1,
                    Name = "foo",
                }
            }.RenderToStringAsync();

            Assert.That(result, Is.EqualTo("var model = {\"Id\":1,\"Name\":\"foo\"};"));

            result = await new PageResult(context.GetPage("page")).RenderToStringAsync();
            Assert.That(result, Is.EqualTo("var model = null;"));

            context.VirtualFiles.WriteFile("page-null.html", "var nil = {{ null | json }};");
            result = await new PageResult(context.GetPage("page-null")).RenderToStringAsync();
            Assert.That(result, Is.EqualTo("var nil = null;"));
        }

        [Test]
        public async Task Does_default_filter_appSetting()
        {
            var context = CreateContext().Init();
            context.AppSettings.Set("copyright", "&copy; 2008-2017 ServiceStack");
            context.VirtualFiles.WriteFile("page.html", "<footer>{{ 'copyright' | appSetting | raw }}</footer>");

            var result = await new PageResult(context.GetPage("page")).RenderToStringAsync();

            Assert.That(result, Is.EqualTo("<footer>&copy; 2008-2017 ServiceStack</footer>"));
        }

        [Test]
        public async Task Does_default_filter_arithmetic_using_filter()
        {
            var context = CreateContext().Init();
            context.VirtualFiles.WriteFile("page.html", @"
1 + 1 = {{ 1 | add(1) }}
2 x 2 = {{ 2 | mul(2) }} or {{ 2 | multiply(2) }}
3 - 3 = {{ 3 | sub(3) }} or {{ 3 | subtract(3) }}
4 / 4 = {{ 4 | div(4) }} or {{ 4 | divide(4) }}");

            var result = await new PageResult(context.GetPage("page")).RenderToStringAsync();

            Assert.That(result.NormalizeNewLines(), Is.EqualTo(@"
1 + 1 = 2
2 x 2 = 4 or 4
3 - 3 = 0 or 0
4 / 4 = 1 or 1
".NormalizeNewLines()));
        }

        [Test]
        public async Task Does_default_filter_arithmetic_without_filter()
        {
            var context = CreateContext().Init();
            context.VirtualFiles.WriteFile("page.html", @"
1 + 1 = {{ add(1,1) }}
2 x 2 = {{ mul(2,2) }} or {{ multiply(2,2) }}
3 - 3 = {{ sub(3,3) }} or {{ subtract(3,3) }}
4 / 4 = {{ div(4,4) }} or {{ divide(4,4) }}");

            var html = await new PageResult(context.GetPage("page")).RenderToStringAsync();

            Assert.That(html.NormalizeNewLines(), Is.EqualTo(@"
1 + 1 = 2
2 x 2 = 4 or 4
3 - 3 = 0 or 0
4 / 4 = 1 or 1
".NormalizeNewLines()));
        }

        [Test]
        public void Can_use_default_filter_arithmetic_with_shorthand_notation()
        {
            var context = new TemplateContext
            {
                Args =
                {
                    ["num"] = 1
                }
            }.Init();

            context.VirtualFiles.WriteFile("page.html", @"
{{ num | add(9) | assignTo('ten') }}
square = {{ 'square-partial' | partial({ ten }) }}
");
            
            context.VirtualFiles.WriteFile("square-partial.html", "{{ ten }} x {{ ten }} = {{ ten | multiply(ten) }}");
            
            Assert.That(new PageResult(context.GetPage("page")).Result.NormalizeNewLines(), Is.EqualTo(@"
square = 10 x 10 = 100".NormalizeNewLines()));
        }
        
        [Test]
        public void Can_incrment_and_decrement()
        {
            var context = new TemplateContext
            {
                Args =
                {
                    ["ten"] = 10
                }
            }.Init();
            
            Assert.That(new PageResult(context.OneTimePage("{{ 1 | incr }}")).Result, Is.EqualTo("2"));
            Assert.That(new PageResult(context.OneTimePage("{{ ten | incr }}")).Result, Is.EqualTo("11"));
            Assert.That(new PageResult(context.OneTimePage("{{ 1 | incrBy(2) }}")).Result, Is.EqualTo("3"));
            Assert.That(new PageResult(context.OneTimePage("{{ ten | incrBy(2) }}")).Result, Is.EqualTo("12"));
            Assert.That(new PageResult(context.OneTimePage("{{ incr(1) }}")).Result, Is.EqualTo("2"));
            Assert.That(new PageResult(context.OneTimePage("{{ incr(ten) }}")).Result, Is.EqualTo("11"));
            Assert.That(new PageResult(context.OneTimePage("{{ incrBy(ten,2) }}")).Result, Is.EqualTo("12"));
            
            Assert.That(new PageResult(context.OneTimePage("{{ 1 | decr }}")).Result, Is.EqualTo("0"));
            Assert.That(new PageResult(context.OneTimePage("{{ ten | decrBy(2) }}")).Result, Is.EqualTo("8"));
        }

        [Test]
        public void Can_compare_numbers()
        {
            var context = new TemplateContext
            {
                Args =
                {
                    ["two"] = 2
                }
            }.Init();
            
            Assert.That(new PageResult(context.OneTimePage("{{ 2 | greaterThan(1) }}")).Result, Is.EqualTo("True"));
            Assert.That(new PageResult(context.OneTimePage("{{ two | greaterThan(1) }}")).Result, Is.EqualTo("True"));
            Assert.That(new PageResult(context.OneTimePage("{{ greaterThan(two,1) }}")).Result, Is.EqualTo("True"));
            Assert.That(new PageResult(context.OneTimePage("{{ greaterThan(2,2) }}")).Result, Is.EqualTo("False"));
            Assert.That(new PageResult(context.OneTimePage("{{ greaterThan(two,2) }}")).Result, Is.EqualTo("False"));
            Assert.That(new PageResult(context.OneTimePage("{{ greaterThan(two,two) }}")).Result, Is.EqualTo("False"));
            
            Assert.That(new PageResult(context.OneTimePage("{{ 'two > 1'    | if(gt(two,1)) | raw }}")).Result, Is.EqualTo("two > 1"));
            Assert.That(new PageResult(context.OneTimePage("{{ 'two > 2'    | if(greaterThan(two,2)) }}")).Result, Is.EqualTo(""));
            Assert.That(new PageResult(context.OneTimePage("{{ 'two > 3'    | if(greaterThan(two,3)) }}")).Result, Is.EqualTo(""));
            Assert.That(new PageResult(context.OneTimePage("{{ 'two > two'  | if(greaterThan(two,two)) }}")).Result, Is.EqualTo(""));
            Assert.That(new PageResult(context.OneTimePage("{{ 'two >= two' | if(greaterThanEqual(two,two)) | raw }}")).Result, Is.EqualTo("two >= two"));

            Assert.That(new PageResult(context.OneTimePage("{{ '1 >= 2' | if(greaterThanEqual(1,2)) }}")).Result, Is.EqualTo(""));
            Assert.That(new PageResult(context.OneTimePage("{{ '2 >= 2' | if(greaterThanEqual(2,2)) | raw }}")).Result, Is.EqualTo("2 >= 2"));
            Assert.That(new PageResult(context.OneTimePage("{{ '3 >= 2' | if(greaterThanEqual(3,2)) | raw }}")).Result, Is.EqualTo("3 >= 2"));

            Assert.That(new PageResult(context.OneTimePage("{{ '1 > 2'  | if(greaterThan(1,2)) }}")).Result, Is.EqualTo(""));
            Assert.That(new PageResult(context.OneTimePage("{{ '2 > 2'  | if(greaterThan(2,2)) }}")).Result, Is.EqualTo(""));
            Assert.That(new PageResult(context.OneTimePage("{{ '3 > 2'  | if(greaterThan(3,2)) | raw }}")).Result, Is.EqualTo("3 > 2"));

            Assert.That(new PageResult(context.OneTimePage("{{ '1 <= 2' | if(lessThanEqual(1,2)) | raw }}")).Result, Is.EqualTo("1 <= 2"));
            Assert.That(new PageResult(context.OneTimePage("{{ '2 <= 2' | if(lessThanEqual(2,2)) | raw }}")).Result, Is.EqualTo("2 <= 2"));
            Assert.That(new PageResult(context.OneTimePage("{{ '3 <= 2' | if(lessThanEqual(3,2)) }}")).Result, Is.EqualTo(""));
            
            Assert.That(new PageResult(context.OneTimePage("{{ '1 < 2'  | if(lessThan(1,2)) | raw }}")).Result, Is.EqualTo("1 < 2"));
            Assert.That(new PageResult(context.OneTimePage("{{ '2 < 2'  | if(lessThan(2,2)) }}")).Result, Is.EqualTo(""));
            Assert.That(new PageResult(context.OneTimePage("{{ '3 < 2'  | if(lessThan(3,2)) }}")).Result, Is.EqualTo(""));
            
            Assert.That(new PageResult(context.OneTimePage("{{ '2 >  2' | if(gt(2,2)) }}")).Result, Is.EqualTo(""));
            Assert.That(new PageResult(context.OneTimePage("{{ '2 >= 2' | if(gte(2,2)) | raw }}")).Result, Is.EqualTo("2 >= 2"));
            Assert.That(new PageResult(context.OneTimePage("{{ '2 <= 2' | if(lte(2,2)) | raw }}")).Result, Is.EqualTo("2 <= 2"));
            Assert.That(new PageResult(context.OneTimePage("{{ '2 <  2' | if(lt(2,2)) }}")).Result, Is.EqualTo(""));
            
            Assert.That(new PageResult(context.OneTimePage("{{ '2 == 2' | if(equals(2,2)) }}")).Result, Is.EqualTo("2 == 2"));
            Assert.That(new PageResult(context.OneTimePage("{{ '2 == 2' | if(eq(2,2)) }}")).Result, Is.EqualTo("2 == 2"));
            Assert.That(new PageResult(context.OneTimePage("{{ '2 != 2' | if(notEquals(2,2)) }}")).Result, Is.EqualTo(""));
            Assert.That(new PageResult(context.OneTimePage("{{ '2 != 2' | if(not(2,2)) }}")).Result, Is.EqualTo(""));
        }

        [Test]
        public void Can_compare_strings()
        {
            var context = new TemplateContext
            {
                Args =
                {
                    ["foo"] = "foo",
                    ["bar"] = "bar",
                }
            }.Init();
            
            Assert.That(new PageResult(context.OneTimePage("{{ 'foo >  \"foo\"' | if(gt(foo,\"foo\")) }}")).Result, Is.EqualTo(""));
            Assert.That(new PageResult(context.OneTimePage("{{ 'foo >= \"foo\"' | if(gte(foo,\"foo\")) | raw }}")).Result, Is.EqualTo("foo >= \"foo\""));
            Assert.That(new PageResult(context.OneTimePage("{{ 'foo <= \"foo\"' | if(lte(foo,\"foo\")) | raw }}")).Result, Is.EqualTo("foo <= \"foo\""));
            Assert.That(new PageResult(context.OneTimePage("{{ 'foo <  \"foo\"' | if(lt(foo,\"foo\")) }}")).Result, Is.EqualTo(""));
            
            Assert.That(new PageResult(context.OneTimePage("{{ 'bar >  \"foo\"' | if(gt(bar,\"foo\")) }}")).Result, Is.EqualTo(""));
            Assert.That(new PageResult(context.OneTimePage("{{ 'bar >= \"foo\"' | if(gte(bar,\"foo\")) | raw }}")).Result, Is.EqualTo(""));
            Assert.That(new PageResult(context.OneTimePage("{{ 'bar <= \"foo\"' | if(lte(bar,\"foo\")) | raw }}")).Result, Is.EqualTo("bar <= \"foo\""));
            Assert.That(new PageResult(context.OneTimePage("{{ 'bar <  \"foo\"' | if(lt(bar,\"foo\")) | raw }}")).Result, Is.EqualTo("bar <  \"foo\""));
        }

        [Test]
        public void Can_compare_DateTime()
        {
            var context = new TemplateContext
            {
                Args =
                {
                    ["year2000"] = new DateTime(2000,1,1),
                    ["year2100"] = new DateTime(2100,1,1),
                }
            }.Init();
            
            Assert.That(new PageResult(context.OneTimePage("{{ 'now >  year2000' | if(gt(now,year2000)) | raw }}")).Result, Is.EqualTo("now >  year2000"));
            Assert.That(new PageResult(context.OneTimePage("{{ 'now >= year2000' | if(gte(now,year2000)) | raw }}")).Result, Is.EqualTo("now >= year2000"));
            Assert.That(new PageResult(context.OneTimePage("{{ 'now <= year2000' | if(lte(now,year2000)) }}")).Result, Is.EqualTo(""));
            Assert.That(new PageResult(context.OneTimePage("{{ 'now <  year2000' | if(lt(now,year2000)) }}")).Result, Is.EqualTo(""));
            
            Assert.That(new PageResult(context.OneTimePage("{{ 'now >  year2100' | if(gt(now,year2100)) }}")).Result, Is.EqualTo(""));
            Assert.That(new PageResult(context.OneTimePage("{{ 'now >= year2100' | if(gte(now,year2100)) }}")).Result, Is.EqualTo(""));
            Assert.That(new PageResult(context.OneTimePage("{{ 'now <= year2100' | if(lte(now,year2100)) | raw }}")).Result, Is.EqualTo("now <= year2100"));
            Assert.That(new PageResult(context.OneTimePage("{{ 'now <  year2100' | if(lt(now,year2100)) | raw }}")).Result, Is.EqualTo("now <  year2100"));
            
            Assert.That(new PageResult(context.OneTimePage("{{ '\"2001-01-01\" >  year2100' | if(gt(\"2001-01-01\",year2100)) }}")).Result, Is.EqualTo(""));
            Assert.That(new PageResult(context.OneTimePage("{{ '\"2001-01-01\" >= year2100' | if(gte(\"2001-01-01\",year2100)) }}")).Result, Is.EqualTo(""));
            Assert.That(new PageResult(context.OneTimePage("{{ '\"2001-01-01\" <= year2100' | if(lte(\"2001-01-01\",year2100)) | raw }}")).Result, Is.EqualTo("\"2001-01-01\" <= year2100"));
            Assert.That(new PageResult(context.OneTimePage("{{ '\"2001-01-01\" <  year2100' | if(lt(\"2001-01-01\",year2100)) | raw }}")).Result, Is.EqualTo("\"2001-01-01\" <  year2100"));
        }

        [Test]
        public void Can_use_logical_boolean_operators()
        {
            var context = new TemplateContext
            {
                Args =
                {
                    ["foo"] = "foo",
                    ["bar"] = "bar",
                    ["year2000"] = new DateTime(2000,1,1),
                    ["year2100"] = new DateTime(2100,1,1),
                    ["contextTrue"] = true,
                    ["contextFalse"] = false,
                }
            }.Init();
            
            Assert.That(new PageResult(context.OneTimePage("{{ 'or(true,true)' | if(or(true,true)) | raw }}")).Result, Is.EqualTo("or(true,true)"));
            Assert.That(new PageResult(context.OneTimePage("{{ 'or(true,false)' | if(or(true,false)) | raw }}")).Result, Is.EqualTo("or(true,false)"));
            Assert.That(new PageResult(context.OneTimePage("{{ 'or(false,false)' | if(or(false,false)) | raw }}")).Result, Is.EqualTo(""));
            
            Assert.That(new PageResult(context.OneTimePage("{{ 'and(true,true)' | if(and(true,true)) | raw }}")).Result, Is.EqualTo("and(true,true)"));
            Assert.That(new PageResult(context.OneTimePage("{{ 'and(true,false)' | if(and(true,false)) | raw }}")).Result, Is.EqualTo(""));
            Assert.That(new PageResult(context.OneTimePage("{{ 'and(false,false)' | if(and(false,false)) | raw }}")).Result, Is.EqualTo(""));
            
            Assert.That(new PageResult(context.OneTimePage("{{ 'or(contextTrue,contextTrue)' | if(or(contextTrue,contextTrue)) | raw }}")).Result, Is.EqualTo("or(contextTrue,contextTrue)"));
            Assert.That(new PageResult(context.OneTimePage("{{ 'or(contextTrue,contextFalse)' | if(or(contextTrue,contextFalse)) | raw }}")).Result, Is.EqualTo("or(contextTrue,contextFalse)"));
            Assert.That(new PageResult(context.OneTimePage("{{ 'or(contextFalse,contextFalse)' | if(or(contextFalse,contextFalse)) | raw }}")).Result, Is.EqualTo(""));
            
            Assert.That(new PageResult(context.OneTimePage("{{ 'or(gt(now,year2000),eq(\"foo\",bar))' | if(or(gt(now,year2000),eq(\"foo\",bar))) | raw }}")).Result, 
                Is.EqualTo("or(gt(now,year2000),eq(\"foo\",bar))"));

            Assert.That(new PageResult(context.OneTimePage(@"{{ 'or(gt(now,year2000),eq(""foo"",bar))' | 
            if (
                or (
                    gt ( now, year2000 ),
                    eq ( ""foo"",  bar )
                )
            ) | raw }}")).Result, 
                Is.EqualTo("or(gt(now,year2000),eq(\"foo\",bar))"));

            
            Assert.That(new PageResult(context.OneTimePage(@"{{ 'or(and(gt(now,year2000),eq(""foo"",bar)),and(gt(now,year2000),eq(""foo"",foo)))' | 
            if ( 
                or (
                    and (
                        gt ( now, year2000 ),
                        eq ( ""foo"", bar  )
                    ),
                    and (
                        gt ( now, year2000 ),
                        eq ( ""foo"", foo  )
                    )
                ) 
            ) | raw }}")).Result, 
                Is.EqualTo(@"or(and(gt(now,year2000),eq(""foo"",bar)),and(gt(now,year2000),eq(""foo"",foo)))"));
        }

        [Test]
        public async Task Does_default_filter_arithmetic_chained_filters()
        {
            var context = CreateContext().Init();

            context.VirtualFiles.WriteFile("page-chained.html",
                @"(((1 + 2) * 3) / 4) - 5 = {{ 1 | add(2) | multiply(3) | divide(4) | subtract(5) }}");
            var result = await new PageResult(context.GetPage("page-chained")).RenderToStringAsync();
            Assert.That(result.NormalizeNewLines(), Is.EqualTo(@"(((1 + 2) * 3) / 4) - 5 = -2.75".NormalizeNewLines()));

            context.VirtualFiles.WriteFile("page-ordered.html",
                @"1 + 2 * 3 / 4 - 5 = {{ 1 | add( divide(multiply(2,3), 4) ) | subtract(5) }}");
            result = await new PageResult(context.GetPage("page-ordered")).RenderToStringAsync();
            Assert.That(result.NormalizeNewLines(), Is.EqualTo(@"1 + 2 * 3 / 4 - 5 = -2.5".NormalizeNewLines()));
        }

        [Test]
        public async Task Does_default_filter_currency()
        {
            var context = CreateContext().Init();
            context.Args[TemplateConstants.DefaultCulture] = new CultureInfo("en-US");

            context.VirtualFiles.WriteFile("page-default.html", "Cost: {{ 99.99 | currency }}");
            context.VirtualFiles.WriteFile("page-culture.html", "Cost: {{ 99.99 | currency(culture) | raw }}");

            var result = await new PageResult(context.GetPage("page-default")).RenderToStringAsync();
            Assert.That(result, Is.EqualTo("Cost: $99.99"));

            result = await new PageResult(context.GetPage("page-culture")) {Args = {["culture"] = "en-AU"}}.RenderToStringAsync();
            Assert.That(result, Is.EqualTo("Cost: $99.99"));

            result = await new PageResult(context.GetPage("page-culture")) {Args = {["culture"] = "en-GB"}}.RenderToStringAsync();
            Assert.That(result, Is.EqualTo("Cost: £99.99"));

            result = await new PageResult(context.GetPage("page-culture")) {Args = {["culture"] = "fr-FR"}}.RenderToStringAsync();
            Assert.That(result, Is.EqualTo("Cost: 99,99 €"));
        }

        [Test]
        public async Task Does_default_filter_format()
        {
            var context = CreateContext().Init();
            context.VirtualFiles.WriteFile("page.html", "{{ 3.14159 | format('N2') }}");
            
            var result = await new PageResult(context.GetPage("page")).RenderToStringAsync();
            Assert.That(result, Is.EqualTo("3.14"));
        }

        [Test]
        public async Task Does_default_filter_dateFormat()
        {
            var context = CreateContext().Init();
            context.VirtualFiles.WriteFile("dateFormat-default.html", "{{ date | dateFormat }}");
            context.VirtualFiles.WriteFile("dateFormat-custom.html", "{{ date | dateFormat(format) }}");
            
            var result = await new PageResult(context.GetPage("dateFormat-default"))
            {
                Args = { ["date"] = new DateTime(2001,01,01,1,1,1,1, DateTimeKind.Utc) }
            }.RenderToStringAsync();
            Assert.That(result, Is.EqualTo("2001-01-01"));

            context.Args[TemplateConstants.DefaultDateFormat] = "dd/MM/yyyy";
            result = await new PageResult(context.GetPage("dateFormat-default"))
            {
                Args = { ["date"] = new DateTime(2001,01,01,1,1,1,1, DateTimeKind.Utc) }
            }.RenderToStringAsync();
            Assert.That(result, Is.EqualTo("01/01/2001"));

            result = await new PageResult(context.GetPage("dateFormat-custom"))
            {
                Args =
                {
                    ["date"] = new DateTime(2001,01,01,1,1,1,1, DateTimeKind.Utc),
                    ["format"] = "dd.MM.yyyy",
                }
            }.RenderToStringAsync();
            Assert.That(result, Is.EqualTo("01.01.2001"));
        }

        [Test]
        public void Does_default_time_format()
        {
            var context = new TemplateContext
            {
                Args =
                {
                    ["time"] = new TimeSpan(1,2,3,4,5),
                    ["date"] = new DateTime(2001,2,3,4,5,6,7),
                }
            }.Init();

            var result = context.EvaluateTemplate("Time: {{ time | timeFormat }}");
            Assert.That(result, Is.EqualTo("Time: 2:03:04"));
            
            result = context.EvaluateTemplate("Time: {{ time | timeFormat('g') }}");
            Assert.That(result, Is.EqualTo("Time: 1:2:03:04.005"));
            
            result = context.EvaluateTemplate("Time: {{ date.TimeOfDay | timeFormat('g') }}");
            Assert.That(result, Is.EqualTo("Time: 4:05:06.007"));

            result = context.EvaluateTemplate("Time: {{ date.TimeOfDay | timeFormat('h\\:mm\\:ss') }}");
            Assert.That(result, Is.EqualTo("Time: 4:05:06"));
        }


        [Test]
        public async Task Does_default_filter_dateTimeFormat()
        {
            var context = CreateContext().Init();
            context.VirtualFiles.WriteFile("dateTimeFormat-default.html", "{{ date | dateTimeFormat }}");
            context.VirtualFiles.WriteFile("dateTimeFormat-custom.html", "{{ date | dateFormat(format) }}");
            
            var result = await new PageResult(context.GetPage("dateTimeFormat-default"))
            {
                Args = { ["date"] = new DateTime(2001,01,01,1,1,1,1, DateTimeKind.Utc) }
            }.RenderToStringAsync();
            Assert.That(result, Is.EqualTo("2001-01-01 01:01:01Z"));

            context.Args[TemplateConstants.DefaultDateTimeFormat] = "dd/MM/yyyy hh:mm";
            result = await new PageResult(context.GetPage("dateTimeFormat-default"))
            {
                Args = { ["date"] = new DateTime(2001,01,01,1,1,1,1, DateTimeKind.Utc) }
            }.RenderToStringAsync();
            Assert.That(result, Is.EqualTo("01/01/2001 01:01"));

            result = await new PageResult(context.GetPage("dateTimeFormat-custom"))
            {
                Args =
                {
                    ["date"] = new DateTime(2001,01,01,1,1,1,1, DateTimeKind.Utc),
                    ["format"] = "dd.MM.yyyy hh.mm.ss",
                }
            }.RenderToStringAsync();
            Assert.That(result, Is.EqualTo("01.01.2001 01.01.01"));
        }

        [Test]
        public void Does_default_spaces_and_indents()
        {
            var context = new TemplateContext().Init();
            
            Assert.That(context.EvaluateTemplate("{{ indent }}"), Is.EqualTo("\t"));
            Assert.That(context.EvaluateTemplate("{{ 4 | indents }}"), Is.EqualTo("\t\t\t\t"));
            
            Assert.That(context.EvaluateTemplate("{{ space }}"), Is.EqualTo(" "));
            Assert.That(context.EvaluateTemplate("{{ 4 | spaces }}"), Is.EqualTo("    "));

            Assert.That(context.EvaluateTemplate("{{ 4 | repeating('  ') }}"), Is.EqualTo("        "));
            Assert.That(context.EvaluateTemplate("{{ '  ' | repeat(4) }}"),    Is.EqualTo("        "));
            Assert.That(context.EvaluateTemplate("{{ '.' | repeat(3) }}"), Is.EqualTo("..."));

            var newLine = Environment.NewLine;
            Assert.That(context.EvaluateTemplate("{{ newLine }}"), Is.EqualTo(newLine));
            Assert.That(context.EvaluateTemplate("{{ 4 | newLines }}"), Is.EqualTo(newLine + newLine + newLine + newLine));
            
            context = new TemplateContext
            {
                Args =
                {
                    [TemplateConstants.DefaultIndent] = "  ",
                    [TemplateConstants.DefaultNewLine] = "\n"
                }
            }.Init();

            Assert.That(context.EvaluateTemplate("{{ indent }}"), Is.EqualTo("  "));
            Assert.That(context.EvaluateTemplate("{{ 4 | newLines }}"), Is.EqualTo("\n\n\n\n"));
        }

        [Test]
        public async Task Does_default_filter_string_filters()
        {
            var context = CreateContext().Init();

            context.VirtualFiles.WriteFile("page-humanize.html", "{{ 'a_varName' | humanize }}");
            var result = await new PageResult(context.GetPage("page-humanize")).RenderToStringAsync();
            Assert.That(result, Is.EqualTo("A Var Name"));

            context.VirtualFiles.WriteFile("page-titleCase.html", "{{ 'war and peace' | titleCase }}");
            result = await new PageResult(context.GetPage("page-titleCase")).RenderToStringAsync();
            Assert.That(result, Is.EqualTo("War And Peace"));

            context.VirtualFiles.WriteFile("page-lower.html", "{{ 'Title Case' | lower }}");
            result = await new PageResult(context.GetPage("page-lower")).RenderToStringAsync();
            Assert.That(result, Is.EqualTo("title case"));

            context.VirtualFiles.WriteFile("page-upper.html", "{{ 'Title Case' | upper }}");
            result = await new PageResult(context.GetPage("page-upper")).RenderToStringAsync();
            Assert.That(result, Is.EqualTo("TITLE CASE"));

            context.VirtualFiles.WriteFile("page-pascalCase.html", "{{ 'camelCase' | pascalCase }}");
            result = await new PageResult(context.GetPage("page-pascalCase")).RenderToStringAsync();
            Assert.That(result, Is.EqualTo("CamelCase"));

            context.VirtualFiles.WriteFile("page-camelCase.html", "{{ 'PascalCase' | camelCase }}");
            result = await new PageResult(context.GetPage("page-camelCase")).RenderToStringAsync();
            Assert.That(result, Is.EqualTo("pascalCase"));

            context.VirtualFiles.WriteFile("page-substring.html", "{{ 'This is a short sentence' | substring(8) }}... {{ 'These three words' | substring(6,5) }}");
            result = await new PageResult(context.GetPage("page-substring")).RenderToStringAsync();
            Assert.That(result, Is.EqualTo("a short sentence... three"));

            context.VirtualFiles.WriteFile("page-pad.html", "<h1>{{ '7' | padLeft(3) }}</h1><h2>{{ 'tired' | padRight(10) }}</h2>");
            result = await new PageResult(context.GetPage("page-pad")).RenderToStringAsync();
            Assert.That(result, Is.EqualTo("<h1>  7</h1><h2>tired     </h2>"));

            context.VirtualFiles.WriteFile("page-padchar.html", "<h1>{{ '7' | padLeft(3,'0') }}</h1><h2>{{ 'tired' | padRight(10,'z') }}</h2>");
            result = await new PageResult(context.GetPage("page-padchar")).RenderToStringAsync();
            Assert.That(result, Is.EqualTo("<h1>007</h1><h2>tiredzzzzz</h2>"));

            context.VirtualFiles.WriteFile("page-repeat.html", "<h1>long time ago{{ ' ...' | repeat(3) }}</h1>");
            result = await new PageResult(context.GetPage("page-repeat")).RenderToStringAsync();
            Assert.That(result, Is.EqualTo("<h1>long time ago ... ... ...</h1>"));
        }

        [Test]
        public void Does_default_filter_with_no_args()
        {
            var context = CreateContext().Init();

            Assert.That(new PageResult(context.OneTimePage("{{ now | dateFormat('yyyy-MM-dd') }}")).Result, Is.EqualTo(DateTime.Now.ToString("yyyy-MM-dd")));
            Assert.That(new PageResult(context.OneTimePage("{{ utcNow | dateFormat('yyyy-MM-dd') }}")).Result, Is.EqualTo(DateTime.UtcNow.ToString("yyyy-MM-dd")));
        }

        [Test]
        public void Can_build_urls_using_filters()
        {
            var context = CreateContext(new Dictionary<string, object>{ {"baseUrl", "http://example.org" }}).Init();

            Assert.That(new PageResult(context.OneTimePage("{{ baseUrl | addPaths(['customers',1,'orders']) | raw }}")).Result, 
                Is.EqualTo("http://example.org/customers/1/orders"));

            Assert.That(new PageResult(context.OneTimePage("{{ baseUrl | addQueryString({ id: 1, foo: 'bar' }) | raw }}")).Result, 
                Is.EqualTo("http://example.org?id=1&foo=bar"));

            Assert.That(new PageResult(context.OneTimePage("{{ baseUrl | addQueryString({ id: 1, foo: 'bar' }) | addHashParams({ hash: 'value' }) | raw }}")).Result, 
                Is.EqualTo("http://example.org?id=1&foo=bar#hash=value"));
        }

        [Test]
        public void Can_assign_result_to_variable()
        {
            string result;
            var context = new TemplateContext
            {
                Args =
                {
                    ["num"] = 1,
                    ["items"] = new[]{ "foo", "bar", "qux" },
                },
                FilterTransformers =
                {
                    ["markdown"] = MarkdownPageFormat.TransformToHtml,
                }
            }.Init();

            result = new PageResult(context.OneTimePage(@"
{{ num | incr | assignTo('result') }}
result={{ result }}
")).Result;
            Assert.That(result.NormalizeNewLines(), Is.EqualTo("result=2"));
            
            result = new PageResult(context.OneTimePage(@"
{{ '<li> {{it}} </li>' | forEach(items) | assignTo('result') }}
<ul>{{ result | raw }}</ul>
")).Result;            
            Assert.That(result.NormalizeNewLines(), Is.EqualTo("<ul><li> foo </li><li> bar </li><li> qux </li></ul>"));
            
            result = new PageResult(context.OneTimePage(@"
{{ ' - {{it}}' | appendLine | forEach(items) | markdown | assignTo('result') }}
<div>{{ result | raw }}</div>
")).Result;            
            Assert.That(result.NormalizeNewLines(), Is.EqualTo("<div><ul>\n<li>foo</li>\n<li>bar</li>\n<li>qux</li>\n</ul>\n</div>"));
        }

        [Test]
        public void Can_assign_to_variables_in_partials()
        {
            var context = new TemplateContext
            {
                Args =
                {
                    ["num"] = 1,
                },
            }.Init();

            context.VirtualFiles.WriteFile("_layout.html", @"
<html>
<body>
<header>
layout num = {{ num }}
pageMetaTitle = {{ pageMetaTitle }}
inlinePageTitle = {{ inlinePageTitle }}
pageResultTitle = {{ pageResultTitle }}
</header>
{{ 'add-partial' | partial({ num: 100 }) }} 
{{ page }}
{{ 'add-partial' | partial({ num: 400 }) }} 
<footer>
layout num = {{ num }}
inlinePageTitle = {{ inlinePageTitle }}
</footer>
</body>
</html>
");
            
            context.VirtualFiles.WriteFile("page.html", @"
<!--
pageMetaTitle: page meta title
-->
<section>
{{ 'page inline title' | upper | assignTo('inlinePageTitle') }}
{{ 'add-partial' | partial({ num: 200 }) }} 
{{ num | add(1) | assignTo('num') }}
<h2>page num = {{ num }}</h2>
{{ 'add-partial' | partial({ num: 300 }) }} 
</section>");
            
            context.VirtualFiles.WriteFile("add-partial.html", @"
{{ num | add(10) | assignTo('num') }}
<h3>partial num = {{ num }}</h3>");
            
            var result = new PageResult(context.GetPage("page"))
            {
                Args =
                {
                    ["pageResultTitle"] = "page result title"
                }
            }.Result;
            
            /* NOTES: 
              1. Page Args and Page Result Args are *always* visible to Layout as they're known before page is executed
              2. Args created during Page execution are *only* visible in Layout after page is rendered (i.e. executed)
              3. Args assigned in partials are retained within their scope
            */
            
            Assert.That(result.RemoveNewLines(), Is.EqualTo(@"
<html>
<body>
<header>
layout num = 1
pageMetaTitle = page meta title
inlinePageTitle = {{ inlinePageTitle }}
pageResultTitle = page result title
</header>
<h3>partial num = 110</h3> 
<section>
<h3>partial num = 210</h3> 
<h2>page num = 2</h2>
<h3>partial num = 310</h3> 
</section>
<h3>partial num = 410</h3> 
<footer>
layout num = 2
inlinePageTitle = PAGE INLINE TITLE
</footer>
</body>
</html>
".RemoveNewLines()));
        }

        [Test]
        public void Does_not_select_template_with_null_target()
        {
            var context = new TemplateContext().Init();

            var result = context.EvaluateTemplate("{{ null | select: was called }}");
            
            Assert.That(result, Is.EqualTo(string.Empty));
        }

        [Test]
        public void Can_parseKeyValueText()
        {
            var context = new TemplateContext
            {
                TemplateFilters = { new TemplateProtectedFilters() }
            }.Init();
            
            context.VirtualFiles.WriteFile("expenses.txt", @"
Rent      1000
TV        50
Internet  50
Mobile    50
Food      400
");

            var output = context.EvaluateTemplate(@"
{{ 'expenses.txt' | includeFile | assignTo: expensesText }}
{{ expensesText | parseKeyValueText | assignTo: expenses }}
Expenses:
{{ expenses | toList | select: { it.Key | padRight(10) }{ it.Value }\n }}
{{ '-' | repeat(15) }}
Total    {{ expenses | values | sum }}
");
            
            Assert.That(output.NormalizeNewLines(), Is.EqualTo(@"
Expenses:
Rent      1000
TV        50
Internet  50
Mobile    50
Food      400

---------------
Total    1550
".NormalizeNewLines()));

        }

        public class ModelValues
        {
            public int Id { get; set; }
            public TimeSpan TimeSpan { get; set; }
            public DateTime DateTime { get; set; }
        }

        [Test]
        public void Can_order_by_different_data_types()
        {
            var items = new[]
            {
                new ModelValues { Id = 1, DateTime = new DateTime(2001,01,01), TimeSpan = TimeSpan.FromSeconds(1) }, 
                new ModelValues { Id = 2, DateTime = new DateTime(2001,01,02), TimeSpan = TimeSpan.FromSeconds(2) },
            };

            var context = new TemplateContext
            {
                Args =
                {
                    ["items"] = items
                }
            }.Init();

            Assert.That(context.EvaluateTemplate(@"{{ items 
                | orderByDescending: it.DateTime 
                | first | property: Id }}"), Is.EqualTo("2"));
            
            Assert.That(context.EvaluateTemplate(@"{{ items 
                | orderByDescending: it.TimeSpan 
                | first | property: Id }}"), Is.EqualTo("2"));
        }

        [Test]
        public void Can_use_not_operator_in_boolean_expression()
        {
            var context = new TemplateContext().Init();
            
            Assert.That(context.EvaluateTemplate(@"
{{ ['A','B','C'] | assignTo: items }}
{{ 'Y' | if(!contains(items, 'A')) | otherwise('N') }}").Trim(), Is.EqualTo("N"));

            Assert.That(context.EvaluateTemplate(@"
{{ ['A','B','C'] | assignTo: items }}
{{ 'Y' | if(!contains(items, 'D')) | otherwise('N') }}").Trim(), Is.EqualTo("Y"));

            Assert.That(context.EvaluateTemplate(@"
{{ ['A','B','C'] | assignTo: items }}
{{ 'Y' | if(not(contains(items, 'D'))) | otherwise('N') }}").Trim(), Is.EqualTo("Y"));

            Assert.That(context.EvaluateTemplate(@"
{{ ['A','B','C'] | assignTo: items }}
{{ ['B','C','D'] | where: !contains(items,it) 
   | first }}").Trim(), Is.EqualTo("D"));
        }

        [Test]
        public void Does_fmt()
        {
            var context = new TemplateContext().Init();
            
            Assert.That(context.EvaluateTemplate("{{ 'in {0} middle' | fmt('the') }}"), 
                Is.EqualTo("in the middle"));
            Assert.That(context.EvaluateTemplate("{{ 'in {0} middle of the {1}' | fmt('the', 'night') }}"), 
                Is.EqualTo("in the middle of the night"));
            Assert.That(context.EvaluateTemplate("{{ 'in {0} middle of the {1} I go {2}' | fmt('the', 'night', 'walking') }}"), 
                Is.EqualTo("in the middle of the night I go walking"));
            Assert.That(context.EvaluateTemplate("{{ 'in {0} middle of the {1} I go {2} in my {3}' | fmt(['the', 'night', 'walking', 'sleep']) }}"), 
                Is.EqualTo("in the middle of the night I go walking in my sleep"));
            
            Assert.That(context.EvaluateTemplate("{{ 'I owe {0:c}' | fmt(123.45) }}"), 
                Is.EqualTo("I owe $123.45"));
        }

        [Test]
        public void Does_appendFmt()
        {
            var context = new TemplateContext().Init();
            
            Assert.That(context.EvaluateTemplate("{{ 'in ' | appendFmt('{0} middle','the') }}"), 
                Is.EqualTo("in the middle"));
            Assert.That(context.EvaluateTemplate("{{ 'in ' | appendFmt('{0} middle of the {1}', 'the', 'night') }}"), 
                Is.EqualTo("in the middle of the night"));
            Assert.That(context.EvaluateTemplate("{{ 'in ' | appendFmt('{0} middle of the {1} I go {2}', 'the', 'night', 'walking') }}"), 
                Is.EqualTo("in the middle of the night I go walking"));
            Assert.That(context.EvaluateTemplate("{{ 'in ' | appendFmt('{0} middle of the {1} I go {2} in my {3}', ['the', 'night', 'walking', 'sleep']) }}"), 
                Is.EqualTo("in the middle of the night I go walking in my sleep"));
            
            Assert.That(context.EvaluateTemplate("{{ 'I ' | appendFmt('owe {0:c}', 123.45) }}"), 
                Is.EqualTo("I owe $123.45"));
        }

        [Test]
        public void Can_use_exist_tests_on_non_existing_arguments()
        {
            var context = new TemplateContext
            {
                Args =
                {
                    ["arg"] = "value" 
                }
            }.Init();
            
            context.VirtualFiles.WriteFile("h1.html", "<h1>{{ it }}</h1>");
            
            
            Assert.That(context.EvaluateTemplate("{{ arg | ifNotNull }}"), Is.EqualTo("value"));
            Assert.That(context.EvaluateTemplate("{{ arg | ifExists }}"), Is.EqualTo("value"));
            Assert.That(context.EvaluateTemplate("{{ noArg | ifNotNull }}"), Is.EqualTo(""));
            Assert.That(context.EvaluateTemplate("{{ noArg | ifExists }}"), Is.EqualTo(""));
            
            Assert.That(context.EvaluateTemplate("{{ arg | selectPartial: h1 }}"), Is.EqualTo("<h1>value</h1>"));
            Assert.That(context.EvaluateTemplate("{{ noArg | selectPartial: h1 }}"), Is.EqualTo(""));
        }

    }
}