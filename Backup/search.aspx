<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="search.aspx.cs" Inherits="clxweb.search" ValidateRequest="false" %>
<!DOCTYPE html>
<html>
<head runat="server">
    <title>Colorissimo Main Page</title>
    <link rel="stylesheet" type="text/css" href="css/styles.css"/>
</head>
<body class="page">
<script type="text/javascript" src="js/script.js"></script>
<center>
<form id="form1" runat="server">
<input style="display:none;" name="parameters" value="<%= Param() %>" />
<input style="display:none;" id="form_event" name="form_event" value="" />
<table border="0" cellpadding="0" cellspacing="0">
<tr><td align="left" style="border-bottom:3px solid #174A82;">

<!--  Logo area -->
<table border="0" cellpadding="6" cellspacing="0">
<tr valign="bottom">
<td class="logo_icon"><div style="height:100px;vertical-align:bottom;"><img src="img/logo1.png" alt="Logo" /></div></td>
<td class="logo_text" width="100%">
<table border="0" cellpadding="3" cellspacing="0">
<tr valign="bottom">
<td style="background-color:#016811;color:#ffffff;">C</td>
<td style="background-color:#cb5815;color:#ffffff;">o</td>
<td style="background-color:#a81d04;color:#ffffff;">l</td>
<td style="background-color:#3f4051;color:#ffffff;">o</td>
<td style="background-color:#642c0b;color:#ffffff;">r</td>
<td style="background-color:#4d2f1b;color:#ffffff;">i</td>
<td style="background-color:#2a5f0d;color:#ffffff;">s</td>
<td style="background-color:#a92d16;color:#ffffff;">s</td>
<td style="background-color:#293067;color:#ffffff;">i</td>
<td style="background-color:#cf4a01;color:#ffffff;">m</td>
<td style="background-color:#294358;color:#ffffff;">o</td>
<td style="background-color:#d31b09;color:#ffffff;">!</td>
</tr>
<tr><td align="left" colspan="12" style="font-size:18px;">Creating palettes from images</td></tr>
</table>
</td>
<td class="logo_text">

<table border="0" cellpadding="0" cellspacing="0">
<tr valign="middle"><td align="center" onclick="document.location.href='add.aspx';"
style="background-color:#909090;cursor:pointer;"><img src="img/plus.png" alt="Add new palette" /></td></tr><tr>
<td align="center"  onclick="document.location.href='add.aspx';"
style="background-color:#909090;color:#ffffff;font-size:x-large;cursor:pointer;padding-left:16px;padding-right:16px;padding-top:0px;padding-bottom:8px;">Add</td>
</tr></table>

</td>
</tr></table>
<!--  Logo area -->


</td></tr>

<tr><td align="center" style="font-size:medium;padding:8px;">
Colors can be entered in following formats: WEB format <b>#452adf</b>, 
RGB byte values (in range 0..255) <b>123,54,32</b> or <b>231 45 23</b>, 
RGB floating point values (in range 0.0..1.0) <b>1.0,0.3,0.1</b> or <b>0.6 0.21 0.95</b>, in form <b>R:0.5 G:0.1 B:0.45</b>, <b>R0.5 G0.23 B1.0</b>, 
<b>R=0.34 G=0.12 B=0.94</b>
</td></tr>

<tr><td align="center">

<!--  Color input -->
<table border="0" cellpadding="2" cellspacing="0"><tr valign="middle">
<td class="color_mark">Color&nbsp;1</td>

<td style="padding-left:8px;padding-right:8px;">
<input type="text" id="color_1" name="color_1" maxlength="128" class="color_input" value="<%= Escape(Param("Color1Text")) %>"
onkeydown="colorChanged(this, 'color_sample_1', 'color_info_1');"
onkeyup="colorChanged(this, 'color_sample_1', 'color_info_1');"
onkeypress="colorChanged(this, 'color_sample_1', 'color_info_1');"
/>
</td>

<td align="center" style="padding:4px;font-size:50px;width:65px;" id="color_sample_1">
<div style="color:#123456;background-color:#123456;">?</div>
</td>

<td class="color_mark">Color&nbsp;2</td>

<td style="padding-left:8px;padding-right:8px;">
<input type="text" id="color_2" name="color_2" maxlength="128" class="color_input" value="<%= Escape(Param("Color2Text")) %>"
onkeydown="colorChanged(this, 'color_sample_2', 'color_info_2');"
onkeyup="colorChanged(this, 'color_sample_2', 'color_info_2');"
onkeypress="colorChanged(this, 'color_sample_2', 'color_info_2');"
/>
</td>

<td align="center" style="padding:4px;font-size:50px;width:65px;" id="color_sample_2">
<div style="color:#000000;background-color:#ffffff;">?</div>
</td>

<td class="color_mark">

<!--  Button -->
<table border="0" cellpadding="0" cellspacing="0" id="search_btn">
<tr valign="middle">
<td class="button_green" title="Click here to search" onclick="disableButton('search_btn');submitEvent('search');">&nbsp;Search</td>
<td class="button_green" title="Click here to search" onclick="disableButton('search_btn');submitEvent('search');"><img src="img/search0.png" /></td>
</tr></table>
<!--  Button -->

</td>
</tr><tr valign="middle">
<td colspan="3" align="center" class="color_info0" id="color_info_1"></td>
<td colspan="3" align="center" class="color_info0" id="color_info_2"></td>
<td class="color_info" >&nbsp;</td>

</tr>
</table>
<!--  Color input -->


</td></tr>

<tr><td align="center" style="padding-top:8px;">
<!--  Search parameters -->
<table border="0" cellpadding="2" cellspacing="0"><tr valign="middle">
<td class="param_title">Color&nbsp;comparison:</td>
<td style="padding-right:16px;"><select name="comparison" class="param_list"><%= RenderComparisonList() %></select></td>
<td class="param_title">Sort&nbsp;by:</td>
<td style="padding-right:16px;"><select name="sort" class="param_list"><%= RenderSortList() %></select></td>
<td class="param_title">Background&nbsp;color:</td>
<td style="padding-right:16px;"><select name="bgcolor" class="param_list"><%= RenderBgColorList() %></select></td>
<td class="param_title"><%= RenderCheckbox("showall", "ShowAll") %></td>
<td class="param_text" style="padding-right:16px;">Show&nbsp;all</td>
<td class="param_title"><%= RenderCheckbox("showsimilar", "ShowSimilar") %></td>
<td class="param_text" style="padding-right:16px;">Show&nbsp;similar</td>
<td class="param_title">&nbsp;</td>
</tr></table>
<!--  Search parameters -->
</td></tr>

<tr valign="top"><td align="center" style="padding-top:16px;">
<!--  Search results -->
<%= RenderSearchResults() %>
<!--  Search results -->
</td></tr>

</table>
</form>

<script type="text/javascript">
    colorChanged(document.getElementById('color_1'), 'color_sample_1', 'color_info_1');
    colorChanged(document.getElementById('color_2'), 'color_sample_2', 'color_info_2');
</script>

</center>

</body>
</html>
