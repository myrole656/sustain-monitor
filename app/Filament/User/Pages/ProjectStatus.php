<?php

namespace App\Filament\User\Pages;

use Filament\Pages\Page;
use App\Filament\User\Widgets\SDG;
use App\Filament\User\Widgets\ProjectDropDown;
use UnitEnum; 
use BackedEnum;

class ProjectStatus extends Page
{
 protected static BackedEnum|string|null $navigationIcon = 'heroicon-o-clipboard-document';    protected static ?string $navigationLabel = 'SDG Status';
    protected string $view = 'filament.pages.project-status';

    protected ?string $heading = 'Sustain Development Goals';

    // Attach widgets here
    protected function getHeaderWidgets(): array
    {
        return [
            ProjectDropDown::class,
            SDG::class,
        ];
    }
}
