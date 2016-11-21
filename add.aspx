<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="add.aspx.cs" Inherits="clxweb.add" ValidateRequest="false" %>
<!DOCTYPE html>
<html>
<head id="Head1" runat="server">
    <title>Colorissimo Add Palette</title>
    <link rel="stylesheet" type="text/css" href="css/styles.css"/>
</head>
<body class="page">
<script type="text/javascript" src="js/script.js"></script>
<center>
<form id="form1" runat="server" method="post" enctype="multipart/form-data">
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
<tr valign="middle"><td align="center" onclick="document.location.href='search.aspx';"
style="background-color:#909090;cursor:pointer;"><img src="img/search0.png" alt="Search palette" /></td></tr><tr>
<td align="center"  onclick="document.location.href='search.aspx';"
style="background-color:#909090;color:#ffffff;font-size:x-large;cursor:pointer;padding-left:16px;padding-right:16px;padding-top:0px;padding-bottom:8px;">Home</td>
</tr></table>

</td>

</tr></table>
<!--  Logo area -->


</td></tr>

<%= RenderError() %>
<%= RenderMessage() %>

<tr><td align="center" style="padding-top:16px;">

<table border="0" cellpadding="2" cellspacing="0">
<tr valign="middle">
<td class="param_title" align="right">Login:</td>
<td align="left" class="param_cell"><input type="text" name="login" maxlength="32" class="color_input" style="width:100px;" value="<%= Escape(Param("Login")) %>" /></td>
</tr>
<tr valign="middle">
<td class="param_title" align="right">Password:</td>
<td align="left" class="param_cell"><input type="password" name="password" maxlength="32" class="color_input" style="width:100px;" value="" /></td>
</tr>
<tr valign="middle">
<td class="param_title" align="left" colspan="2" style="border-right:3px solid #909090;padding-right:10px;">Description:</td>
</tr>
<tr valign="middle">
<td class="param_title" align="left" colspan="2"style="border-right:3px solid #909090;padding-right:10px;">
<textarea rows="5" cols="100" name="description" class="color_input" style="width:300px;"><%= Escape(Param("Description"))%></textarea>
</td>
</tr>
<tr valign="middle">
<td class="param_title" align="right">Processing:</td>
<td align="left" class="param_cell"><select name="processing" class="param_list"><%= RenderProcessingList() %></select></td>
</tr>
<tr valign="middle">
<td class="param_title" align="left" colspan="2" style="border-right:3px solid #909090;padding-right:10px;">Image&nbsp;file:</td>
</tr>
<tr valign="middle">
<td class="param_title" align="left" colspan="2" style="border-right:3px solid #909090;padding-right:10px;">
<input type="file" name="image" size="30" class="color_input" style="width:400px;"/>
</td></tr>
<tr><td align="center" colspan="2"class="param_title" style="border-right:3px solid #909090;padding-right:10px;padding-top:16px;">

<table border="0" cellpadding="0" cellspacing="0" id="add_btn">
<tr valign="middle">
<td class="button_green" title="Click here to search" onclick="disableButton('add_btn');submitEvent('addfile');">&nbsp;Add</td>
<td class="button_green" title="Click here to search" onclick="disableButton('add_btn');submitEvent('addfile');"><img src="img/plus.png" /></td>
</tr></table>

</td></tr>
</table>

</td></tr>



</table>
</form>
</center>    
</body>
</html>
