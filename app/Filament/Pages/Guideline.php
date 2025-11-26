<?php

namespace App\Filament\Pages;

use Filament\Pages\Page;
use UnitEnum; 
use BackedEnum;
class Guideline extends Page
{
    protected string $view = 'filament.user.pages.guideline';
    protected static BackedEnum|string|null $navigationIcon = 'heroicon-o-academic-cap';
}



