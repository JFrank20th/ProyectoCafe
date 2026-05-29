import { merge } from 'lodash-es';

import type { Theme } from '@mui/material/styles';

import Alert from './Alert';
import Avatar from './Avatar';
import Button from './Button';
import CardActions from './CardActions';
import CardContent from './CardContent';
import CardHeader from './CardHeader';
import Checkbox from './Checkbox';
import Chip from './Chip';
import DataGrid from './DataGrid';
import DatePicker from './DatePicker';
import DateTimePickerToolbar from './DateTimePickerToolbar';
import Dialog from './Dialog';
import DialogTitle from './DialogTitle';
import Divider from './Divider';
import InputBase from './InputBase';
import ListItemButton from './ListItemButton';
import ListItemIcon from './ListItemIcon';
import ListItemText from './ListItemText';
import OutlinedInput from './OutlinedInput';
import Paper from './Paper';
import Select from './Select';
import Slider from './Slider';
import TableCell from './TableCell';
import TableRow from './TableRow';
import Tabs from './Tabs';
import Typography from './Typography';

export default function componentsOverrides(theme: Theme, borderRadius: number, outlinedFilled: boolean): Theme['components'] {
  return merge(
    Alert(theme),
    Avatar(theme),
    Button(theme),
    CardActions(),
    CardContent(),
    CardHeader(theme),
    Checkbox(),
    Chip(theme),
    DataGrid(theme),
    DatePicker(),
    DateTimePickerToolbar(),
    Dialog(theme),
    DialogTitle(theme),
    Divider(theme),
    InputBase(theme),
    ListItemButton(theme),
    ListItemIcon(theme),
    ListItemText(theme),
    OutlinedInput(theme, borderRadius, outlinedFilled),
    Paper(borderRadius),
    Select(),
    Slider(theme),
    TableCell(theme),
    TableRow(theme),
    Tabs(theme),
    Typography(theme)
  ) as Theme['components'];
}
